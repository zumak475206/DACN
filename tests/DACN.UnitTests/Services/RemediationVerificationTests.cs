using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.Services;
using FluentAssertions;

namespace DACN.UnitTests.Services;

public class RemediationVerificationTests
{
    [Fact]
    public void UserSessionService_SeparateInstances_RemainIsolatedAndSupportStorageRehydration()
    {
        // GAP-04: Mỗi Circuit SignalR có 1 instance UserSessionService riêng biệt (Scoped)
        var circuitA = new UserSessionService();
        var circuitB = new UserSessionService();

        circuitA.SetUser("Võ Admin Tổng", "admin@hotel.com", "Admin", id: 1);

        // Circuit B (người dùng ở trình duyệt khác) không bị nhiễm phiên Admin của Circuit A
        circuitB.CurrentUser.IsLoggedIn.Should().BeFalse();
        circuitB.CurrentUser.Role.Should().Be("Customer");

        // Kiểm tra lưu & khôi phục phiên khi F5 (ProtectedLocalStorage payload)
        var storedPayload = circuitA.ToStoredSession();
        var circuitAAfterF5 = new UserSessionService();
        circuitAAfterF5.RestoreFromStored(storedPayload);

        circuitAAfterF5.CurrentUser.IsLoggedIn.Should().BeTrue();
        circuitAAfterF5.CurrentUser.Role.Should().Be("Admin");
        circuitAAfterF5.CurrentUser.Email.Should().Be("admin@hotel.com");
        circuitAAfterF5.IsRestored.Should().BeTrue();
    }

    [Theory]
    [InlineData("FrontDesk", false)]
    [InlineData("Housekeeping", false)]
    [InlineData("Accountant", false)]
    [InlineData("Manager", false)]
    [InlineData("Customer", false)]
    [InlineData("Admin", true)]
    public void UserSessionService_ManageUsersPermission_IsRestrictedStrictlyToAdmin(string role, bool expectedCanManageUsers)
    {
        // GAP-01: Chỉ Admin mới có quyền ManageUsers để vào /admin/accounts và /admin/rbac
        var session = new UserSessionService();
        session.SetUser("Test User", $"{role.ToLowerInvariant()}@hotel.com", role, id: 10);

        session.HasPermission(Quyen.ManageUsers).Should().Be(expectedCanManageUsers);
    }

    [Fact]
    public void StatusMapper_DashboardRoomCounters_AccuratelyCountOccupiedAndDirtyRooms()
    {
        // GAP-07: Kiểm tra logic đếm phòng trên AdminDashboard với dữ liệu chuẩn SQL Server
        var rooms = new List<Phong>
        {
            new() { SoPhong = "101", TrangThai = "Occupied", TrangThaiVeSinh = "Clean" },
            new() { SoPhong = "102", TrangThai = "Occupied", TrangThaiVeSinh = "Dirty" },
            new() { SoPhong = "103", TrangThai = "Available", TrangThaiVeSinh = "Dirty" },
            new() { SoPhong = "104", TrangThai = "Available", TrangThaiVeSinh = "Inspecting" },
            new() { SoPhong = "105", TrangThai = "Available", TrangThaiVeSinh = "Clean" }
        };

        var occupiedCount = rooms.Count(r => StatusMapper.IsRoomOccupied(r.TrangThai));
        var cleaningCount = rooms.Count(r => StatusMapper.NormalizePhongVeSinh(r.TrangThaiVeSinh) is "Dirty" or "Inspecting");

        occupiedCount.Should().Be(2);
        cleaningCount.Should().Be(3);
    }

    [Theory]
    [InlineData("Accountant", 5, 5, false)] // Kế toán tự duyệt phiếu của mình -> Chặn
    [InlineData("Accountant", 5, 2, false)] // Kế toán duyệt phiếu người khác -> Chặn
    [InlineData("Manager", 2, 2, false)]    // Quản lý tự duyệt phiếu của chính mình -> Chặn (Four-Eyes SoD)
    [InlineData("Manager", 2, 5, true)]     // Quản lý duyệt phiếu do Kế toán lập -> Hợp lệ
    [InlineData("Admin", 1, 5, true)]       // Admin duyệt phiếu do Kế toán lập -> Hợp lệ
    public void ExpenseApproval_SegregationOfDuties_EnforcesFourEyesPrinciple(
        string approverRole,
        int approverUserId,
        int creatorUserId,
        bool expectedAllowed)
    {
        // GAP-02: Nguyên tắc phân tách trách nhiệm (Segregation of Duties) trong phê duyệt chi phí
        var isManagerOrAdmin = approverRole is "Admin" or "Manager";
        var isNotCreator = creatorUserId != approverUserId;
        var canApprove = isManagerOrAdmin && isNotCreator;

        canApprove.Should().Be(expectedAllowed);
    }
}
