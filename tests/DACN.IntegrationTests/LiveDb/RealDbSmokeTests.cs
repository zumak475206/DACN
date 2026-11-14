using DACN.Infrastructure.Data;
using DACN.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DACN.IntegrationTests.LiveDb;

/// <summary>
/// Các bài kiểm tra kết nối TRỰC TIẾP vào CSDL thật (SQL Server - Database: DACN)
/// Kiểm chứng tính toàn vẹn của Schema, Entity Framework Core mappings,
/// và các tác vụ nghiệp vụ thực tế trên DB thực.
/// </summary>
public class RealDbSmokeTests
{
    private const string ConnectionString = "Server=.;Database=DACN;Trusted_Connection=True;TrustServerCertificate=True;";

    private static DACNContext CreateRealDbContext()
    {
        var options = new DbContextOptionsBuilder<DACNContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new DACNContext(options);
    }

    [Fact]
    public async Task RealDb_CanConnect_AndQueryLoaiPhong()
    {
        await using var db = CreateRealDbContext();

        // Kiểm tra khả năng kết nối
        var canConnect = await db.Database.CanConnectAsync();
        canConnect.Should().BeTrue("Hệ thống phải kết nối được tới SQL Server local với database DACN");

        // Truy vấn dữ liệu Loại Phòng thực tế
        var loaiPhongs = await db.LoaiPhongs
            .AsNoTracking()
            .Include(lp => lp.Phongs)
            .ToListAsync();

        loaiPhongs.Should().NotBeNull();
        loaiPhongs.Should().NotBeEmpty("Database DACN cần có ít nhất 1 loại phòng");
    }

    [Fact]
    public async Task RealDb_CanQuery_NguoiDung_WithRoles()
    {
        await using var db = CreateRealDbContext();

        var usersWithRoles = await db.NguoiDungs
            .AsNoTracking()
            .Include(u => u.MaVaiTroNavigation)
            .ToListAsync();

        usersWithRoles.Should().NotBeNull();
        usersWithRoles.Should().NotBeEmpty("Database DACN cần có người dùng được seed sẵn");
        usersWithRoles.Should().Contain(u => u.Email == "admin@hotel.com");
    }

    [Fact]
    public async Task RealDb_AuditService_CanWriteAndRead_FromNhatKyHeThong()
    {
        await using var db = CreateRealDbContext();
        var auditService = new AuditService(db, NullLogger<AuditService>.Instance);

        var testId = Guid.NewGuid().ToString("N")[..8];
        var actionName = $"REAL_DB_TEST_{testId}";

        // 1. Ghi log kiểm toán vào Database thực
        await auditService.GhiNhanAsync(
            hanhDong: actionName,
            doiTuong: "RealDbSmokeTest",
            khoaChinh: testId,
            noiDung: "Kiểm tra ghi log trực tiếp vào DB thực DACN",
            maNguoiDung: 1,
            email: "admin@hotel.com",
            diaChiIp: "127.0.0.1",
            thanhCong: true);

        // 2. Đọc lại từ DB thực
        var (danhSach, tongSo) = await auditService.LayDanhSachAsync(new Web.DTOs.Audit.NhatKyFilterRequest
        {
            HanhDong = actionName,
            Trang = 1,
            SoLuong = 10
        });

        tongSo.Should().BeGreaterOrEqualTo(1);
        danhSach.Should().Contain(x => x.HanhDong == actionName && x.KhoaChinh == testId);

        // Cleanup: Xóa log test vừa tạo để giữ DB sạch
        var testLog = await db.NhatKyHeThongs.FirstOrDefaultAsync(x => x.HanhDong == actionName);
        if (testLog != null)
        {
            db.NhatKyHeThongs.Remove(testLog);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task RealDb_RefreshToken_CanInsertQueryAndRevoke()
    {
        await using var db = CreateRealDbContext();

        // Lấy admin user có sẵn trong DB thực
        var admin = await db.NguoiDungs.FirstAsync(u => u.Email == "admin@hotel.com");

        var testTokenString = $"test_token_{Guid.NewGuid():N}";
        var refreshToken = new RefreshToken
        {
            MaNguoiDung = admin.MaId,
            Token = testTokenString,
            HetHanVao = DateTime.UtcNow.AddDays(7),
            NgayTao = DateTime.UtcNow,
            DiaChiIpTao = "127.0.0.1",
            DaThuHoi = false
        };

        // 1. Thêm token vào DB thực
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        // 2. Truy vấn lại từ DB thực
        var fetched = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == testTokenString);
        fetched.Should().NotBeNull();
        fetched!.ConHieuLuc.Should().BeTrue();
        fetched.MaNguoiDung.Should().Be(admin.MaId);

        // 3. Thu hồi token
        fetched.DaThuHoi = true;
        fetched.NgayThuHoi = DateTime.UtcNow;
        fetched.LyDoThuHoi = "Test cleanup";
        await db.SaveChangesAsync();

        // 4. Verify đã thu hồi
        var revoked = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == testTokenString);
        revoked!.ConHieuLuc.Should().BeFalse();
        revoked.DaThuHoi.Should().BeTrue();

        // Cleanup: Xóa token test
        db.RefreshTokens.Remove(revoked);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task RealDb_FullBookingLifecycle_SatisfiesAllSqlCheckConstraints()
    {
        await using var db = CreateRealDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();

        var room = await db.Phongs.FirstAsync();
        var loaiPhongId = room.MaLoaiPhong;

        // 1. Tạo Đặt Phòng + Chi Tiết Đặt Phòng + Hóa Đơn (kiểm tra CHECK constraints của DatPhong, ChiTietDatPhong, HoaDon)
        var datPhong = new DatPhong
        {
            MaDatPhong = $"DP-TEST-{Guid.NewGuid():N}"[..18],
            TenKhach = "Khách Test SQL Constraint",
            Sdtkhach = "0905111222",
            EmailKhach = "sqltest@hotel.com",
            KenhDatPhong = Web.Common.StatusMapper.NormalizeKenhDatPhong("website"), // -> 'web'
            TrangThai = Web.Common.StatusMapper.NormalizeDatPhongTrangThai("cho_xac_nhan"), // -> 'Pending'
            TongTien = 1500000m,
            TienDatCoc = 450000m,
            NgayTao = DateTime.UtcNow,
            NgayCapNhat = DateTime.UtcNow
        };
        db.DatPhongs.Add(datPhong);
        await db.SaveChangesAsync();

        var chiTiet = new ChiTietDatPhong
        {
            MaDatPhong = datPhong.MaId,
            MaLoaiPhong = loaiPhongId,
            MaPhong = room.MaId,
            NgayNhanPhongDuKien = DateTime.UtcNow.Date.AddDays(1),
            NgayTraPhongDuKien = DateTime.UtcNow.Date.AddDays(3),
            GiaMoiDem = 750000m,
            TrangThai = Web.Common.StatusMapper.NormalizeChiTietDatPhongTrangThai("cho_xac_nhan") // -> 'Pending'
        };
        db.ChiTietDatPhongs.Add(chiTiet);

        var hoaDon = new HoaDon
        {
            MaDatPhong = datPhong.MaId,
            TongTienPhong = 1500000m,
            TongTienDichVu = 0,
            TienGiamGia = 0,
            TienThue = 0,
            TienPhuThu = 0,
            TongThanhToan = 1500000m,
            TrangThai = Web.Common.StatusMapper.NormalizeHoaDonTrangThai("chua_thanh_toan"), // -> 'Pending'
            NgayTao = DateTime.UtcNow
        };
        db.HoaDons.Add(hoaDon);
        await db.SaveChangesAsync();

        // 2. Check-in (DatPhong -> 'CheckedIn', ChiTietDatPhong -> 'Active', Phong -> 'Occupied')
        datPhong.TrangThai = "CheckedIn";
        chiTiet.TrangThai = "Active";
        room.TrangThai = "Occupied";
        await db.SaveChangesAsync();

        // 3. Đặt dịch vụ & Ghi nhận hư hỏng (DonDichVu -> 'Pending', MatMatHuHong -> cộng phụ thu HoaDon)
        var donDv = new DonDichVu
        {
            MaChiTietDatPhong = chiTiet.MaId,
            TrangThai = "Pending",
            TongTien = 200000m,
            NgayDatDichVu = DateTime.UtcNow
        };
        db.DonDichVus.Add(donDv);

        var huHong = new MatMatHuHong
        {
            MaChiTietDatPhong = chiTiet.MaId,
            SoLuong = 1,
            TienPhat = 100000m,
            MoTa = "Vỡ ly thủy tinh",
            NgayTao = DateTime.UtcNow
        };
        db.MatMatHuHongs.Add(huHong);
        hoaDon.TongTienDichVu += 200000m;
        hoaDon.TienPhuThu += 100000m;
        hoaDon.TongThanhToan = 1800000m;
        await db.SaveChangesAsync();

        // 4. Thanh toán (ThanhToan.PhuongThucThanhToan -> 'VNPay', TrangThai -> 'Success', HoaDon -> 'Paid')
        var tt = new ThanhToan
        {
            MaHoaDon = hoaDon.MaId,
            PhuongThucThanhToan = Web.Common.StatusMapper.NormalizePhuongThucThanhToan("vnpay"),
            SoTienDaThanhToan = 1800000m,
            MaGiaoDich = $"VNP-{Guid.NewGuid():N}"[..16],
            TrangThai = "Success",
            NgayThanhToan = DateTime.UtcNow
        };
        db.ThanhToans.Add(tt);
        hoaDon.TrangThai = "Paid";
        await db.SaveChangesAsync();

        // 5. Check-out & phát sinh NhatKyBuongPhong ('CheckedOut', Phong -> 'Available'/'Dirty', NhatKyBuongPhong -> 'assigned' -> 'done')
        datPhong.TrangThai = "CheckedOut";
        chiTiet.TrangThai = "CheckedOut";
        room.TrangThai = "Available";
        room.TrangThaiVeSinh = "Dirty";

        var nhatKyBp = new NhatKyBuongPhong
        {
            MaPhong = room.MaId,
            MaChiTietDatPhong = chiTiet.MaId,
            TrangThai = "assigned",
            ThoiGianPhanCong = DateTime.UtcNow,
            NgayTao = DateTime.UtcNow
        };
        db.NhatKyBuongPhongs.Add(nhatKyBp);
        await db.SaveChangesAsync();

        nhatKyBp.TrangThai = "done";
        nhatKyBp.ThoiGianHoanTat = DateTime.UtcNow;
        room.TrangThaiVeSinh = "Clean";
        await db.SaveChangesAsync();

        nhatKyBp.MaId.Should().BeGreaterThan(0);

        // Rollback để không làm bẩn dữ liệu thật trong DACN
        await tx.RollbackAsync();
    }
}
