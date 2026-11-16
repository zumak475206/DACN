using DACN.Web.Common;

namespace DACN.Web.Services;

public class CurrentUserInfo
{
    public int Id { get; set; } = 1;
    public string HoTen { get; set; } = "Võ Admin Tổng";
    public string Email { get; set; } = "admin@hotel.com";
    public string Role { get; set; } = "Admin"; // Admin, Manager, FrontDesk, Housekeeping, Customer
    public HashSet<string> Permissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string RoleDisplayName => Role switch
    {
        "Admin" => "Tổng Quản Trị (Admin)",
        "Manager" => "Quản Lý Khách Sạn (Manager / BI)",
        "FrontDesk" => "Nhân Viên Lễ Tân (Front-Desk)",
        "Housekeeping" => "Nhân Viên Buồng Phòng",
        "Accountant" => "Kế Toán (Accountant)",
        _ => "Khách Hàng (Customer)"
    };
    public string RoleShortBadge => Role switch
    {
        "Admin" => "Admin",
        "Manager" => "Quản Lý",
        "FrontDesk" => "Lễ Tân",
        "Housekeeping" => "Buồng Phòng",
        "Accountant" => "Kế Toán",
        _ => "Thành Viên"
    };
    public bool IsLoggedIn { get; set; } = false;
}

public class UserSessionService
{
    public CurrentUserInfo CurrentUser { get; private set; }

    public event Action? OnChange;

    public UserSessionService()
    {
        CurrentUser = new CurrentUserInfo
        {
            Id = 0,
            HoTen = "Khách Vãng Lai",
            Email = "",
            Role = "Customer",
            Permissions = new HashSet<string>(new[] { Quyen.UseAIChatbot }, StringComparer.OrdinalIgnoreCase),
            IsLoggedIn = false
        };
    }

    public void SetUser(string hoTen, string email, string role, int id = 1, IEnumerable<string>? permissions = null)
    {
        var defaultPerms = GetDefaultPermissionsForRole(role);
        if (permissions != null)
        {
            var custom = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
            if (custom.Count > 0) defaultPerms = custom;
        }

        CurrentUser = new CurrentUserInfo
        {
            Id = id,
            HoTen = hoTen,
            Email = email,
            Role = NormalizeRoleCode(role),
            Permissions = defaultPerms,
            IsLoggedIn = true
        };
        NotifyStateChanged();
    }

    public void SwitchRole(string role)
    {
        CurrentUser = CreateRoleUser(role);
        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = new CurrentUserInfo
        {
            Id = 0,
            HoTen = "Khách Vãng Lai",
            Email = "",
            Role = "Customer",
            Permissions = new HashSet<string>(new[] { Quyen.UseAIChatbot }, StringComparer.OrdinalIgnoreCase),
            IsLoggedIn = false
        };
        NotifyStateChanged();
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission)) return false;
        if (CurrentUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return true;
        return CurrentUser.Permissions.Contains(permission);
    }

    public bool CanAccessAdmin() => CurrentUser.Role == "Admin" || HasPermission(Quyen.ManageUsers) || HasPermission(Quyen.ManageVouchers) || HasPermission(Quyen.ManageArticles);
    public bool CanAccessAiBi() => CurrentUser.Role == "Admin" || CurrentUser.Role == "Manager" || HasPermission(Quyen.ViewDashboard) || HasPermission(Quyen.ViewAIPredictions);
    public bool CanAccessFrontDesk() => CurrentUser.Role == "Admin" || CurrentUser.Role == "FrontDesk" || CurrentUser.Role == "Manager" || CurrentUser.Role == "Accountant" || HasPermission(Quyen.ManageBookings) || HasPermission(Quyen.ManageInvoices);
    public bool CanAccessHousekeeping() => CurrentUser.Role == "Admin" || CurrentUser.Role == "Housekeeping" || CurrentUser.Role == "Manager" || HasPermission(Quyen.ManageHousekeeping);

    public string GetDefaultStaffRoute() => CurrentUser.Role switch
    {
        "Admin" => "/admin/dashboard",
        "Manager" => "/ai-analytics/dashboard",
        "FrontDesk" => "/frontdesk/bookings",
        "Housekeeping" => "/housekeeping/rooms",
        "Accountant" => "/frontdesk/services-billing",
        _ => "/admin/dashboard"
    };

    private static string NormalizeRoleCode(string role)
    {
        var r = (role ?? string.Empty).ToLowerInvariant();
        if (r.Contains("admin") || r.Contains("quản trị")) return "Admin";
        if (r.Contains("manager") || r.Contains("quản lý")) return "Manager";
        if (r.Contains("reception") || r.Contains("front") || r.Contains("lễ tân")) return "FrontDesk";
        if (r.Contains("housekeep") || r.Contains("buồng phòng")) return "Housekeeping";
        if (r.Contains("account") || r.Contains("kế toán")) return "Accountant";
        return "Customer";
    }

    private static CurrentUserInfo CreateRoleUser(string role)
    {
        var normalized = NormalizeRoleCode(role);
        return normalized switch
        {
            "Customer" => new CurrentUserInfo
            {
                Id = 4,
                HoTen = "Nguyễn Văn Khách",
                Email = "khachhang@gmail.com",
                Role = "Customer",
                Permissions = GetDefaultPermissionsForRole("Customer"),
                IsLoggedIn = true
            },
            "FrontDesk" => new CurrentUserInfo
            {
                Id = 2,
                HoTen = "Trần Lễ Tân",
                Email = "letan@hotel.com",
                Role = "FrontDesk",
                Permissions = GetDefaultPermissionsForRole("FrontDesk"),
                IsLoggedIn = true
            },
            "Housekeeping" => new CurrentUserInfo
            {
                Id = 3,
                HoTen = "Lê Thị Buồng Phòng",
                Email = "buongphong@hotel.com",
                Role = "Housekeeping",
                Permissions = GetDefaultPermissionsForRole("Housekeeping"),
                IsLoggedIn = true
            },
            "Manager" => new CurrentUserInfo
            {
                Id = 5,
                HoTen = "Hoàng Quản Lý",
                Email = "manager@hotel.com",
                Role = "Manager",
                Permissions = GetDefaultPermissionsForRole("Manager"),
                IsLoggedIn = true
            },
            _ => new CurrentUserInfo
            {
                Id = 1,
                HoTen = "Võ Admin Tổng",
                Email = "admin@hotel.com",
                Role = "Admin",
                Permissions = GetDefaultPermissionsForRole("Admin"),
                IsLoggedIn = true
            }
        };
    }

    private static HashSet<string> GetDefaultPermissionsForRole(string role)
    {
        var normalized = NormalizeRoleCode(role);
        return normalized switch
        {
            "Admin" => new HashSet<string>(new[]
            {
                Quyen.ViewDashboard, Quyen.ManageRooms, Quyen.ManageBookings, Quyen.CheckInOut,
                Quyen.ManageInvoices, Quyen.ManagePayments, Quyen.ManageServices, Quyen.OrderRoomService,
                Quyen.ManageHousekeeping, Quyen.UpdateRoomStatus, Quyen.ManageUsers, Quyen.ManageVouchers,
                Quyen.ManageArticles, Quyen.ModerateReviews, Quyen.UseAIChatbot, Quyen.ViewAIPredictions,
                Quyen.ManageExpenses, Quyen.ManageAIData
            }, StringComparer.OrdinalIgnoreCase),
            "Manager" => new HashSet<string>(new[]
            {
                Quyen.ViewDashboard, Quyen.ManageRooms, Quyen.ManageBookings, Quyen.CheckInOut,
                Quyen.ManageInvoices, Quyen.ManagePayments, Quyen.ManageServices, Quyen.OrderRoomService,
                Quyen.ManageHousekeeping, Quyen.UpdateRoomStatus, Quyen.ManageVouchers,
                Quyen.ManageArticles, Quyen.ModerateReviews, Quyen.UseAIChatbot, Quyen.ViewAIPredictions,
                Quyen.ManageExpenses, Quyen.ManageAIData
            }, StringComparer.OrdinalIgnoreCase),
            "FrontDesk" => new HashSet<string>(new[]
            {
                Quyen.ManageBookings, Quyen.CheckInOut, Quyen.ManageInvoices, Quyen.ManagePayments,
                Quyen.OrderRoomService, Quyen.UpdateRoomStatus, Quyen.UseAIChatbot
            }, StringComparer.OrdinalIgnoreCase),
            "Housekeeping" => new HashSet<string>(new[]
            {
                Quyen.ManageHousekeeping, Quyen.UpdateRoomStatus
            }, StringComparer.OrdinalIgnoreCase),
            _ => new HashSet<string>(new[] { Quyen.UseAIChatbot }, StringComparer.OrdinalIgnoreCase)
        };
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
