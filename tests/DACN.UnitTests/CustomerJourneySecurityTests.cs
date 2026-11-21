using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.Controllers;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.DatPhong;
using DACN.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DACN.UnitTests;

public class CustomerJourneySecurityTests
{
    private static DACNContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<DACNContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DACNContext(options);
    }

    private static DatPhongController CreateControllerWithUser(DACNContext db, int userId, string role, params string[] permissions)
    {
        var auditService = Substitute.For<IAuditService>();
        var controller = new DatPhongController(db, auditService);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Email, "customer@test.com")
        };
        foreach (var p in permissions)
        {
            claims.Add(new Claim("permission", p));
        }

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task TraCuuKhachVangLai_WithMatchingCodeAndPhone_ReturnsBooking()
    {
        await using var db = CreateInMemoryDb();
        db.DatPhongs.Add(new DatPhong
        {
            MaId = 10,
            MaDatPhong = "DP-WEB-2611201800",
            TenKhach = "Trần Minh Khoa",
            Sdtkhach = "0905 888 999",
            EmailKhach = "khoa@gmail.com",
            KenhDatPhong = "web",
            TrangThai = "Pending",
            TongTien = 1_500_000m,
            NgayTao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateControllerWithUser(db, 0, "Anonymous");
        var res = await controller.TraCuuKhachVangLai(new TraCuuDatPhongRequest
        {
            MaDatPhong = "dp-web-2611201800",
            ThongTinLienHe = "0905888999"
        });

        var ok = res.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<DatPhongDto>>().Subject;
        payload.ThanhCong.Should().BeTrue();
        payload.DuLieu!.MaDatPhong.Should().Be("DP-WEB-2611201800");
    }

    [Fact]
    public async Task TraCuuKhachVangLai_WithWrongContact_ReturnsNotFound_PreventingDataLeak()
    {
        await using var db = CreateInMemoryDb();
        db.DatPhongs.Add(new DatPhong
        {
            MaId = 11,
            MaDatPhong = "DP-WEB-2611201801",
            TenKhach = "Lê Thị Bảo Mật",
            Sdtkhach = "0912345678",
            EmailKhach = "baomat@gmail.com",
            KenhDatPhong = "web",
            TrangThai = "Confirmed",
            TongTien = 2_000_000m,
            NgayTao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateControllerWithUser(db, 0, "Anonymous");
        var res = await controller.TraCuuKhachVangLai(new TraCuuDatPhongRequest
        {
            MaDatPhong = "DP-WEB-2611201801",
            ThongTinLienHe = "0999999999"
        });

        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task HuyDatPhong_CustomerCancellingWithin48HoursAndPast15MinGrace_ReturnsBadRequest()
    {
        await using var db = CreateInMemoryDb();
        var booking = new DatPhong
        {
            MaId = 20,
            MaDatPhong = "DP-LATE-CANCEL",
            MaNguoiDung = 100,
            KenhDatPhong = "web",
            TrangThai = "Confirmed",
            TongTien = 1_000_000m,
            NgayTao = DateTime.UtcNow.AddHours(-2) // Đã tạo 2 tiếng trước (qua 15 phút ân hạn)
        };
        booking.ChiTietDatPhongs.Add(new ChiTietDatPhong
        {
            MaId = 201,
            MaDatPhong = 20,
            MaLoaiPhong = 1,
            NgayNhanPhongDuKien = DateTime.UtcNow.Date.AddDays(1), // Nhận phòng ngày mai (< 48h)
            NgayTraPhongDuKien = DateTime.UtcNow.Date.AddDays(2),
            GiaMoiDem = 1_000_000m,
            TrangThai = "Confirmed"
        });
        db.DatPhongs.Add(booking);
        await db.SaveChangesAsync();

        var controller = CreateControllerWithUser(db, 100, "Customer");
        var res = await controller.HuyDatPhong(20, new HuyDatPhongRequest { LyDoHuy = "Đổi lịch sát giờ" });

        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HuyDatPhong_CustomerCancellingBeyond48Hours_ReturnsOk()
    {
        await using var db = CreateInMemoryDb();
        var booking = new DatPhong
        {
            MaId = 21,
            MaDatPhong = "DP-EARLY-CANCEL",
            MaNguoiDung = 100,
            KenhDatPhong = "web",
            TrangThai = "Confirmed",
            TongTien = 1_000_000m,
            NgayTao = DateTime.UtcNow.AddHours(-5)
        };
        booking.ChiTietDatPhongs.Add(new ChiTietDatPhong
        {
            MaId = 202,
            MaDatPhong = 21,
            MaLoaiPhong = 1,
            NgayNhanPhongDuKien = DateTime.UtcNow.Date.AddDays(5), // Nhận phòng sau 5 ngày (>= 48h)
            NgayTraPhongDuKien = DateTime.UtcNow.Date.AddDays(6),
            GiaMoiDem = 1_000_000m,
            TrangThai = "Confirmed"
        });
        db.DatPhongs.Add(booking);
        await db.SaveChangesAsync();

        var controller = CreateControllerWithUser(db, 100, "Customer");
        var res = await controller.HuyDatPhong(21, new HuyDatPhongRequest { LyDoHuy = "Hủy đúng hạn trước 48h" });

        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TaoDatPhong_WhenUnpaidHoldIsOlderThan15Minutes_DoesNotBlockInventory()
    {
        await using var db = CreateInMemoryDb();
        db.LoaiPhongs.Add(new LoaiPhong
        {
            MaId = 1,
            Ten = "VIP Ocean Suite",
            DuongDanRutGon = "vip-ocean-suite",
            GiaCoBan = 2_000_000m,
            SoNguoiLonToiDa = 2,
            DangHoatDong = true
        });
        db.Phongs.Add(new Phong
        {
            MaId = 101,
            SoPhong = "901",
            MaLoaiPhong = 1,
            TrangThai = "Available",
            TrangThaiVeSinh = "Clean"
        });

        // Đơn giữ chỗ 0đ tạo cách đây 30 phút (đã hết hạn giữ chỗ 15 phút)
        var expiredHold = new DatPhong
        {
            MaId = 30,
            MaDatPhong = "DP-SPAM-HOLD",
            KenhDatPhong = "web",
            TrangThai = "Pending",
            TienDatCoc = 0m,
            TongTien = 2_000_000m,
            NgayTao = DateTime.UtcNow.AddMinutes(-30)
        };
        expiredHold.ChiTietDatPhongs.Add(new ChiTietDatPhong
        {
            MaId = 301,
            MaDatPhong = 30,
            MaLoaiPhong = 1,
            NgayNhanPhongDuKien = DateTime.UtcNow.Date.AddDays(3),
            NgayTraPhongDuKien = DateTime.UtcNow.Date.AddDays(4),
            GiaMoiDem = 2_000_000m,
            TrangThai = "Pending"
        });
        db.DatPhongs.Add(expiredHold);
        await db.SaveChangesAsync();

        var controller = CreateControllerWithUser(db, 105, "Customer");
        var res = await controller.TaoDatPhong(new TaoDatPhongRequest
        {
            TenKhach = "Khách Hàng Thật",
            SdtKhach = "0905111222",
            EmailKhach = "real@gmail.com",
            DanhSachPhong =
            [
                new ChiTietDatPhongRequest
                {
                    MaLoaiPhong = 1,
                    NgayNhanPhong = DateTime.UtcNow.Date.AddDays(3),
                    NgayTraPhong = DateTime.UtcNow.Date.AddDays(4)
                }
            ]
        });

        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
