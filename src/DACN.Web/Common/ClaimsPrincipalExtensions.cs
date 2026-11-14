using System.Security.Claims;

namespace DACN.Web.Common;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }

    public static string? GetUserRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role);
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        var role = (user.GetUserRole() ?? string.Empty).ToLowerInvariant();
        return role.Contains("admin") || role.Contains("quan tri") || role.Contains("quản trị");
    }

    public static bool IsManagerOrAdmin(this ClaimsPrincipal user)
    {
        if (user.IsAdmin()) return true;
        var role = (user.GetUserRole() ?? string.Empty).ToLowerInvariant();
        return role.Contains("manager") || role.Contains("quan ly") || role.Contains("quản lý");
    }

    public static bool IsStaffOrAdmin(this ClaimsPrincipal user)
    {
        if (user.IsManagerOrAdmin()) return true;
        var role = (user.GetUserRole() ?? string.Empty).ToLowerInvariant();
        var loai = (user.FindFirstValue("loai_nguoi_dung") ?? user.FindFirstValue("LoaiNguoiDung") ?? string.Empty).ToLowerInvariant();
        return role.Contains("receptionist") || role.Contains("le tan") || role.Contains("lễ tân")
            || role.Contains("housekeeper") || role.Contains("buong phong") || role.Contains("buồng phòng")
            || role.Contains("accountant") || role.Contains("ke toan") || role.Contains("kế toán")
            || role.Contains("nhan vien") || role.Contains("nhân viên")
            || role.Contains("thu ngan") || role.Contains("thu ngân")
            || loai == "nhan_vien" || loai == "staff" || loai == "admin";
    }

    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        if (user.IsAdmin()) return true;
        var permissions = user.FindAll("permission").Select(c => c.Value);
        if (permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase)))
            return true;
        return user.IsStaffOrAdmin();
    }
}
