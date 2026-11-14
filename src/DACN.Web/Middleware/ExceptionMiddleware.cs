using System.Net;
using System.Text.Json;
using DACN.Web.DTOs.Common;

namespace DACN.Web.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi không xử lý: {Message}", ex.Message);
            await XuLyLoi(context, ex, env);
        }
    }

    private static async Task XuLyLoi(HttpContext context, Exception ex, IHostEnvironment env)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var message = env.IsDevelopment()
            ? $"Lỗi hệ thống: {ex.Message} {(ex.InnerException != null ? "--> " + ex.InnerException.Message : "")}"
            : "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

        var response = ApiResponse<object>.Fail(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
