using System.Text;
using System.Threading.RateLimiting;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.Components;
using DACN.Web.DTOs.Common;
using DACN.Web.Middleware;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. EF Core & Database Connection String (Ưu tiên nạp từ biến môi trường để bảo mật)
var connectionString = Environment.GetEnvironmentVariable("DACN_CONNECTION_STRING")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Không tìm thấy chuỗi kết nối cơ sở dữ liệu. Vui lòng cấu hình ConnectionStrings:DefaultConnection hoặc biến môi trường DACN_CONNECTION_STRING.");
}

builder.Services.AddDbContext<DACNContext>(options =>
    options.UseSqlServer(connectionString));

// Đăng ký dịch vụ Audit Log kiểm toán hệ thống & AI Travel Advisor
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<GoiYDuLichService>();
builder.Services.AddSingleton<UserSessionService>();

// 2. CORS Policy (Cross-Origin Resource Sharing)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5014",
                "https://localhost:7136")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. Rate Limiting (Chống Brute-force & DoS)
builder.Services.AddRateLimiter(options =>
{
    // Chính sách nghiêm ngặt cho Auth (Đăng nhập, Đăng ký, Đổi mật khẩu): 10 req/phút/IP
    options.AddPolicy("auth-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Chính sách giới hạn cho các endpoint công khai nhạy cảm (kiểm tra voucher, chat AI): 20 req/phút/IP
    options.AddPolicy("public-strict", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"strict_{clientIp}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Chính sách tiêu chuẩn cho toàn bộ API: 100 req/phút/IP
    options.AddPolicy("general-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2
            });
    });

    // Phản hồi chuẩn khi vi phạm giới hạn tốc độ (HTTP 429)
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        var res = ApiResponse<object>.Fail("Hệ thống phát hiện quá nhiều yêu cầu từ bạn. Vui lòng thử lại sau 1 phút.");
        await context.HttpContext.Response.WriteAsJsonAsync(res, cancellationToken: token);
    };
});

// 4. JWT Authentication (Hỗ trợ nạp từ Biến môi trường & Kiểm tra độ mạnh)
var jwtKey = Environment.GetEnvironmentVariable("DACN_JWT_SECRET")
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? builder.Configuration["Jwt:Key"]
    ?? "DACN_Hotel_Management_Super_Secret_Key_2026_@_DeepMind_Security";

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DACN_HotelManagement";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DACN_HotelManagement_Clients";

if (builder.Environment.IsProduction())
{
    if (jwtKey.Contains("DeepMind_Security") || jwtKey.Length < 32)
    {
        throw new InvalidOperationException("LỖI BẢO MẬT: Môi trường Production bắt buộc phải cấu hình biến môi trường DACN_JWT_SECRET có độ dài tối thiểu 32 ký tự và không dùng key mặc định.");
    }
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Chuẩn hóa lỗi Validation Model tự động (Trả về ApiResponse đồng nhất)
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
            .ToList();

        var message = errors.Count > 0
            ? string.Join("; ", errors)
            : "Dữ liệu yêu cầu không hợp lệ.";

        return new BadRequestObjectResult(ApiResponse<object>.Fail(message));
    };
});

// 6. Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DACN Hotel Management API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Token theo cú pháp: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 7. Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DACNContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await SchemaMigrationRunner.EnsureLatestSchemaAsync(db, logger);
}

// Pipeline Middleware
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DACN API v1"));
}

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseRateLimiter();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// API routes áp dụng General Rate Limit mặc định
app.MapControllers().RequireRateLimiting("general-policy");

// Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
