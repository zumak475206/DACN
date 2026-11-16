using System.Security.Claims;
using DACN.Web.Common;
using FluentAssertions;

namespace DACN.UnitTests.Common;

public class ClaimsPrincipalExtensionsTests
{
    // ─── Helper ─────────────────────────────────────────────────────────────

    private static ClaimsPrincipal MakeUser(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal MakeUser(string? nameIdentifier = null,
                                             string? role = null,
                                             string? loaiNguoiDung = null)
    {
        var claims = new List<Claim>();
        if (nameIdentifier != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
        if (loaiNguoiDung != null) claims.Add(new Claim("LoaiNguoiDung", loaiNguoiDung));
        return MakeUser(claims);
    }

    // ─── GetUserId ───────────────────────────────────────────────────────────

    [Fact]
    public void GetUserId_ReturnsNull_WhenNoClaimPresent()
    {
        var user = MakeUser();
        user.GetUserId().Should().BeNull();
    }

    [Fact]
    public void GetUserId_ReturnsUserId_WhenClaimValid()
    {
        var user = MakeUser(nameIdentifier: "42");
        user.GetUserId().Should().Be(42);
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenClaimNotInteger()
    {
        var user = MakeUser(nameIdentifier: "not-a-number");
        user.GetUserId().Should().BeNull();
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenClaimIsEmptyString()
    {
        var user = MakeUser(nameIdentifier: "");
        user.GetUserId().Should().BeNull();
    }

    // ─── GetUserRole ─────────────────────────────────────────────────────────

    [Fact]
    public void GetUserRole_ReturnsNull_WhenNoRoleClaim()
    {
        var user = MakeUser();
        user.GetUserRole().Should().BeNull();
    }

    [Fact]
    public void GetUserRole_ReturnsRole_WhenClaimPresent()
    {
        var user = MakeUser(role: "admin");
        user.GetUserRole().Should().Be("admin");
    }

    // ─── IsStaffOrAdmin ──────────────────────────────────────────────────────

    [Fact]
    public void IsStaffOrAdmin_ReturnsFalse_WhenNoClaims()
    {
        var user = MakeUser();
        user.IsStaffOrAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsFalse_ForCustomerLoaiNguoiDung_AndNoAdminRole()
    {
        var user = MakeUser(role: "khach_hang", loaiNguoiDung: "customer");
        user.IsStaffOrAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsFalse_ForKhachHangLoaiNguoiDung_AndNoAdminRole()
    {
        var user = MakeUser(role: "khach_hang", loaiNguoiDung: "khach_hang");
        user.IsStaffOrAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForCustomerLoaiNguoiDung_WhenHasAdminRole()
    {
        // customer + admin role → vẫn được xem là staff/admin
        var user = MakeUser(role: "admin", loaiNguoiDung: "customer");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForAdminRole()
    {
        var user = MakeUser(role: "admin");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForAdminRole_CaseInsensitive()
    {
        var user = MakeUser(role: "Admin");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForLeTanRole()
    {
        var user = MakeUser(role: "le tan");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForNhanVienRole()
    {
        var user = MakeUser(role: "nhan vien");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForKeToanRole()
    {
        var user = MakeUser(role: "ke toan");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForThuNganRole()
    {
        var user = MakeUser(role: "thu ngan");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForAdminLoaiNguoiDung()
    {
        var user = MakeUser(loaiNguoiDung: "admin");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForStaffLoaiNguoiDung()
    {
        var user = MakeUser(loaiNguoiDung: "staff");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsStaffOrAdmin_ReturnsTrue_ForNhanVienLoaiNguoiDung()
    {
        var user = MakeUser(loaiNguoiDung: "nhan_vien");
        user.IsStaffOrAdmin().Should().BeTrue();
    }

    // ─── HasPermission (Chống leo thang đặc quyền RBAC) ──────────────────────

    [Fact]
    public void HasPermission_ReturnsFalse_WhenStaffDoesNotHavePermissionClaim()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Receptionist"),
            new("LoaiNguoiDung", "staff"),
            new("permission", Quyen.ManageBookings)
        };
        var user = MakeUser(claims);

        user.HasPermission(Quyen.ManageUsers).Should().BeFalse();
        user.HasPermission(Quyen.ManageVouchers).Should().BeFalse();
        user.HasPermission(Quyen.ManageBookings).Should().BeTrue();
    }

    [Fact]
    public void HasPermission_ReturnsTrue_AlwaysForAdmin()
    {
        var user = MakeUser(role: "Admin");
        user.HasPermission(Quyen.ManageUsers).Should().BeTrue();
        user.HasPermission(Quyen.ManageVouchers).Should().BeTrue();
        user.HasPermission(Quyen.ViewAIPredictions).Should().BeTrue();
    }

    // ─── GioVN.TaoSlug & GoiYDuLichService Scoring / NLU ─────────────────────

    [Fact]
    public void GioVN_TaoSlug_RemovesVietnameseDiacriticsAndSpecialChars()
    {
        var slug = GioVN.TaoSlug("Khám Phá Bán Đảo Sơn Trà & Biển Mỹ Khê 2026!");
        slug.Should().Be("kham-pha-ban-dao-son-tra-bien-my-khe-2026");
    }

    [Fact]
    public void GoiYDuLichService_ThangNamTrongKhoang_HandlesCrossYearSeason()
    {
        // Mùa đông từ tháng 11 đến tháng 2 năm sau
        DACN.Web.Services.GoiYDuLichService.ThangNamTrongKhoang(12, 11, 2).Should().BeTrue();
        DACN.Web.Services.GoiYDuLichService.ThangNamTrongKhoang(1, 11, 2).Should().BeTrue();
        DACN.Web.Services.GoiYDuLichService.ThangNamTrongKhoang(6, 11, 2).Should().BeFalse();
    }

    [Fact]
    public void GoiYDuLichService_TrichXuatYChinh_ExtractsBudgetMonthAndVibe()
    {
        var intent = DACN.Web.Services.GoiYDuLichService.TrichXuatYChinh(
            "Tháng 6 này mình có 3 triệu muốn đi Đà Nẵng chụp ảnh check-in sống ảo và ăn hải sản");

        intent.ChuDe.Should().Be("goi_y_du_lich");
        intent.ThangDi.Should().Be(6);
        intent.NganSach.Should().Be(3_000_000m);
        intent.KhuVuc.Should().Be("central");
        intent.PhongCach.Should().Contain("songao");
        intent.PhongCach.Should().Contain("amthuc");
    }
}
