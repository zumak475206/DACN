using DACN.Infrastructure.Data;
using DACN.Web.DTOs.Audit;
using DACN.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DACN.UnitTests.Services;

public class AuditServiceTests
{
    private static DACNContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DACNContext>()
            .UseInMemoryDatabase(databaseName: $"AuditTestDb_{Guid.NewGuid()}")
            .Options;

        return new DACNContext(options);
    }

    [Fact]
    public async Task GhiNhanAsync_SuccessfullySavesAuditLogToDatabase()
    {
        using var db = CreateInMemoryDbContext();
        var logger = Substitute.For<ILogger<AuditService>>();
        var service = new AuditService(db, logger);

        await service.GhiNhanAsync(
            hanhDong: "TEST_ACTION",
            doiTuong: "Room",
            khoaChinh: "101",
            noiDung: "Cập nhật giá phòng",
            maNguoiDung: 1,
            email: "admin@hotel.com",
            diaChiIp: "127.0.0.1",
            thanhCong: true);

        var savedLog = await db.NhatKyHeThongs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.HanhDong.Should().Be("TEST_ACTION");
        savedLog.DoiTuong.Should().Be("Room");
        savedLog.KhoaChinh.Should().Be("101");
        savedLog.Email.Should().Be("admin@hotel.com");
        savedLog.MaNguoiDung.Should().Be(1);
        savedLog.ThanhCong.Should().BeTrue();
    }

    [Fact]
    public async Task LayDanhSachAsync_PaginatesAndFiltersCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var logger = Substitute.For<ILogger<AuditService>>();
        var service = new AuditService(db, logger);

        // Seed 5 records
        for (int i = 1; i <= 5; i++)
        {
            db.NhatKyHeThongs.Add(new NhatKyHeThong
            {
                HanhDong = i % 2 == 0 ? "LOGIN" : "LOGOUT",
                DoiTuong = "Auth",
                Email = $"user{i}@test.com",
                ThanhCong = true,
                ThoiGian = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        // Filter by HanhDong = LOGIN, Trang = 1, SoLuong = 10
        var (danhSach, tongSo) = await service.LayDanhSachAsync(new NhatKyFilterRequest
        {
            HanhDong = "LOGIN",
            Trang = 1,
            SoLuong = 10
        });

        tongSo.Should().Be(2);
        danhSach.Should().HaveCount(2);
        danhSach.Should().AllSatisfy(x => x.HanhDong.Should().Be("LOGIN"));
    }

    [Fact]
    public async Task LayDanhSachAsync_FiltersByEmail()
    {
        using var db = CreateInMemoryDbContext();
        var logger = Substitute.For<ILogger<AuditService>>();
        var service = new AuditService(db, logger);

        db.NhatKyHeThongs.AddRange(
            new NhatKyHeThong { HanhDong = "A", DoiTuong = "T", Email = "alice@example.com", ThanhCong = true, ThoiGian = DateTime.UtcNow },
            new NhatKyHeThong { HanhDong = "B", DoiTuong = "T", Email = "bob@example.com", ThanhCong = true, ThoiGian = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var (danhSach, tongSo) = await service.LayDanhSachAsync(new NhatKyFilterRequest
        {
            Email = "alice",
            Trang = 1,
            SoLuong = 10
        });

        tongSo.Should().Be(1);
        danhSach.First().Email.Should().Be("alice@example.com");
    }
}
