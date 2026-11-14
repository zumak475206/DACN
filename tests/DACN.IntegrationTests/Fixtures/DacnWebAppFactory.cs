using DACN.Infrastructure.Data;
using DACN.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DACN.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory thay thế SQL Server bằng InMemory DB và mock AuditService.
/// Mỗi instance dùng DB riêng (Guid) để test song song không conflict.
/// </summary>
public class DacnWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set dummy connection string để Program.cs không throw
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=InMemory;Database=Test;");
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // 1. Xóa toàn bộ DbContext registrations (bao gồm cả SQL Server provider internals)
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<DACNContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(DACNContext) ||
                    d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // 2. Thêm InMemory DB (mỗi factory instance dùng DB name riêng)
            services.AddDbContext<DACNContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // 3. Mock IAuditService để tránh ExecuteSqlRawAsync lỗi với InMemory
            var auditDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuditService));
            if (auditDescriptor != null) services.Remove(auditDescriptor);
            services.AddScoped<IAuditService>(_ =>
            {
                var mock = Substitute.For<IAuditService>();
                mock.GhiNhanAsync(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
                    Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(),
                    Arg.Any<string?>(), Arg.Any<bool>())
                    .Returns(Task.CompletedTask);
                return mock;
            });
        });
    }

    /// <summary>
    /// Seed dữ liệu trực tiếp vào InMemory DB của factory này.
    /// </summary>
    public void SeedDatabase(Action<DACNContext> seeder)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DACNContext>();
        seeder(db);
        db.SaveChanges();
    }
}
