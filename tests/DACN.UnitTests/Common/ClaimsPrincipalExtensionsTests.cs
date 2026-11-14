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
}
