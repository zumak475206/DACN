using System;

namespace DACN.Web.Services;

public class CurrentUserInfo
{
    public int Id { get; set; } = 1;
    public string HoTen { get; set; } = "Trần Lễ Tân";
    public string Email { get; set; } = "letan@grandhotel.vn";
    public string Role { get; set; } = "Admin"; // Admin, Manager, FrontDesk, Housekeeping, Customer
    public string RoleDisplayName => Role switch
    {
        "Admin" => "Tổng Quản Trị (Admin)",
        "Manager" => "Quản Lý Khách Sạn (Manager / BI)",
        "FrontDesk" => "Nhân Viên Lễ Tân (Front-Desk)",
        "Housekeeping" => "Nhân Viên Buồng Phòng",
        _ => "Khách Hàng (Customer)"
    };
    public bool IsLoggedIn { get; set; } = true;
}

public class UserSessionService
{
    public CurrentUserInfo CurrentUser { get; private set; } = new();

    public event Action? OnChange;

    public void SetUser(string hoTen, string email, string role, int id = 1)
    {
        CurrentUser = new CurrentUserInfo
        {
            Id = id,
            HoTen = hoTen,
            Email = email,
            Role = role,
            IsLoggedIn = true
        };
        NotifyStateChanged();
    }

    public void SwitchRole(string role)
    {
        switch (role)
        {
            case "Customer":
                SetUser("Nguyễn Văn Khách", "khachhang@gmail.com", "Customer", 4);
                break;
            case "FrontDesk":
                SetUser("Trần Lễ Tân", "letan@grandhotel.vn", "FrontDesk", 2);
                break;
            case "Housekeeping":
                SetUser("Lê Thị Buồng Phòng", "buongphong@grandhotel.vn", "Housekeeping", 3);
                break;
            case "Manager":
                SetUser("Hoàng Quản Lý", "quanly@grandhotel.vn", "Manager", 5);
                break;
            default:
                SetUser("Võ Admin Tổng", "admin@grandhotel.vn", "Admin", 1);
                break;
        }
    }

    public void Logout()
    {
        CurrentUser = new CurrentUserInfo
        {
            Id = 0,
            HoTen = "Khách Vãng Lai",
            Email = "",
            Role = "Customer",
            IsLoggedIn = false
        };
        NotifyStateChanged();
    }

    public bool CanAccessAdmin() => CurrentUser.Role == "Admin";
    public bool CanAccessAiBi() => CurrentUser.Role == "Admin" || CurrentUser.Role == "Manager";
    public bool CanAccessFrontDesk() => CurrentUser.Role == "Admin" || CurrentUser.Role == "FrontDesk" || CurrentUser.Role == "Manager";
    public bool CanAccessHousekeeping() => CurrentUser.Role == "Admin" || CurrentUser.Role == "Housekeeping" || CurrentUser.Role == "Manager";

    private void NotifyStateChanged() => OnChange?.Invoke();
}
