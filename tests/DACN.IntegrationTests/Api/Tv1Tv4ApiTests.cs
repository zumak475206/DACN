using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DACN.Infrastructure.Data;
using DACN.IntegrationTests.Fixtures;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.Dashboard;
using DACN.Web.DTOs.Voucher;
using DACN.Web.Services;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace DACN.IntegrationTests.Api;

public class Tv1Tv4ApiTests
{
    private const string JwtSecret = "DACN_Hotel_Management_Super_Secret_Key_2026_@_DeepMind_Security";

    private static string CreateTestJwt(int userId, string role, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, $"user{userId}@hotel.com"),
            new(ClaimTypes.Name, $"Test {role}"),
            new(ClaimTypes.Role, role),
            new("LoaiNguoiDung", role == "Customer" ? "customer" : "staff")
        };

        foreach (var p in permissions)
            claims.Add(new Claim("permission", p));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "DACN_HotelManagement",
            audience: "DACN_HotelManagement_Clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Rbac_ReceptionistWithoutManageUsers_CannotChangeUserRole_Returns403()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var receptionistToken = CreateTestJwt(2, "Receptionist", Quyen.ManageBookings, Quyen.CheckInOut);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", receptionistToken);

        var resp = await client.PutAsJsonAsync("api/v1/nguoi-dung/2/vai-tro", new
        {
            MaVaiTro = 1,
            LoaiNguoiDung = "staff"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rbac_HousekeeperWithoutManageVouchers_CannotCreateVoucher_Returns403()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var housekeeperToken = CreateTestJwt(3, "Housekeeper", Quyen.ManageHousekeeping, Quyen.UpdateRoomStatus);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", housekeeperToken);

        var resp = await client.PostAsJsonAsync("api/v1/voucher", new TaoVoucherRequest
        {
            MaCode = "HACKVOUCHER",
            LoaiGiamGia = "percent",
            GiaTriGiamGia = 50,
            HieuLucTu = DateTime.UtcNow.AddDays(-1),
            HieuLucDen = DateTime.UtcNow.AddDays(10),
            GioiHanSuDung = 10
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Voucher_CongKhai_And_MaxDiscountCap_WorksCorrectly()
    {
        await using var factory = new DacnWebAppFactory();
        factory.SeedDatabase(db =>
        {
            db.MaGiamGia.Add(new MaGiamGium
            {
                MaCode = "CAP20",
                LoaiGiamGia = "percent",
                GiaTriGiamGia = 20,
                GiaTriDatPhongToiThieu = 1_000_000,
                GiamToiDa = 300_000, // Giảm 20% nhưng tối đa 300k
                HieuLucTu = GioVN.Now.AddDays(-2),
                HieuLucDen = GioVN.Now.AddDays(15),
                GioiHanSuDung = 50,
                SoLanDaDung = 0,
                DangHoatDong = true,
                NgayTao = DateTime.UtcNow
            });
        });

        var client = factory.CreateClient();

        // 1. Lấy danh sách voucher công khai
        var pubResp = await client.GetAsync("api/v1/voucher/cong-khai");
        pubResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Kiểm tra mức trần giảm tối đa (20% của 3.000.000 là 600k -> bị chặn trần ở 300k)
        var checkResp = await client.PostAsJsonAsync("api/v1/voucher/kiem-tra", new KiemTraVoucherRequest
        {
            MaCode = "CAP20",
            TongTienDatPhong = 3_000_000
        });

        checkResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await checkResp.Content.ReadFromJsonAsync<ApiResponse<KiemTraVoucherResponse>>();
        body!.DuLieu!.HopLe.Should().BeTrue();
        body.DuLieu.TienGiam.Should().Be(300_000m);
    }

    [Fact]
    public async Task Tv4_AiGoiY_And_HybridChat_ReturnsScoredSuggestions()
    {
        await using var factory = new DacnWebAppFactory();
        factory.SeedDatabase(db =>
        {
            db.MuaDuLiches.Add(new MuaDuLich
            {
                MaId = 1,
                Ten = "Mùa Hè Biển Đẹp",
                LoaiMua = "summer",
                ThangBatDau = 5,
                ThangKetThuc = 8,
                KhuVuc = "central",
                LaMuaCaoDiem = true,
                NgayTao = DateTime.UtcNow
            });

            db.GoiYtourAis.Add(new GoiYtourAi
            {
                MaId = 1,
                TieuDe = "Tour Sống Ảo Biển Mỹ Khê & Sơn Trà 3N2Đ",
                DiemDen = "Đà Nẵng",
                GiaToiThieu = 2_200_000m,
                GiaToiDa = 3_500_000m,
                SoNgay = 3,
                MuaDuLich = "summer",
                PhongCach = "songao,thugian",
                DiemDanhGiaTrungBinh = 4.9m,
                DangHoatDong = true,
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow
            });

            db.LoaiPhongs.Add(new LoaiPhong
            {
                MaId = 1,
                Ten = "Deluxe Ocean View",
                DuongDanRutGon = "deluxe-ocean-view",
                GiaCoBan = 850_000m,
                SoNguoiLonToiDa = 2,
                DangHoatDong = true
            });
        });

        var client = factory.CreateClient();

        // 1. Test POST /api/v1/ai/goi-y
        var goiYResp = await client.PostAsJsonAsync("api/v1/ai/goi-y", new GoiYDuLichRequest
        {
            NganSach = 3_000_000m,
            ThangDi = 6,
            PhongCach = "songao",
            SoDem = 2
        });

        goiYResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var goiYBody = await goiYResp.Content.ReadFromJsonAsync<ApiResponse<List<GoiYKetQuaDto>>>();
        goiYBody!.ThanhCong.Should().BeTrue();
        goiYBody.DuLieu.Should().NotBeEmpty();
        goiYBody.DuLieu![0].PhanTramPhuHop.Should().BeGreaterThanOrEqualTo(85);
        goiYBody.DuLieu[0].HangPhongPhuHop.Should().Be("Deluxe Ocean View");

        // 2. Test POST /api/v1/ai/chat
        var chatResp = await client.PostAsJsonAsync("api/v1/ai/chat", new
        {
            CauHoi = "Tháng 6 mình có 3 triệu muốn đi Đà Nẵng check-in sống ảo"
        });

        chatResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var chatBody = await chatResp.Content.ReadFromJsonAsync<ApiResponse<ChatAiKetQuaDto>>();
        chatBody!.ThanhCong.Should().BeTrue();
        chatBody.DuLieu!.MaPhienChat.Should().StartWith("CHAT-");
        chatBody.DuLieu.YChinh.NganSach.Should().Be(3_000_000m);
        chatBody.DuLieu.YChinh.ThangDi.Should().Be(6);
        chatBody.DuLieu.GoiYKemTheo.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Tv4_DashboardKpi_ReturnsHotelBiMetrics()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var managerToken = CreateTestJwt(5, "Manager", Quyen.ViewDashboard, Quyen.ViewAIPredictions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        var resp = await client.GetAsync("api/v1/dashboard/kpi");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<HotelBiKpiDto>>();
        body!.ThanhCong.Should().BeTrue();
        body.DuLieu.Should().NotBeNull();
    }

    [Fact]
    public async Task Rbac_Housekeeper_CannotCreateLoaiPhong_Returns403()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var housekeeperToken = CreateTestJwt(3, "Housekeeper", Quyen.ManageHousekeeping, Quyen.UpdateRoomStatus);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", housekeeperToken);

        var resp = await client.PostAsJsonAsync("api/v1/loai-phong", new
        {
            Ten = "Phòng Hacked",
            DuongDanRutGon = "phong-hacked",
            GiaCoBan = 100_000,
            SoNguoiLonToiDa = 2
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rbac_Accountant_CannotCheckInBooking_Returns403()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var accountantToken = CreateTestJwt(4, "Accountant", Quyen.ViewDashboard, Quyen.ManageInvoices, Quyen.ManagePayments, Quyen.ManageExpenses);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accountantToken);

        var resp = await client.PutAsync("api/v1/dat-phong/1/check-in", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rbac_Housekeeper_CannotViewFinancialDashboardKpi_Returns403()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var housekeeperToken = CreateTestJwt(3, "Housekeeper", Quyen.ManageHousekeeping, Quyen.UpdateRoomStatus);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", housekeeperToken);

        var resp = await client.GetAsync("api/v1/dashboard/kpi");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rbac_FrontDesk_CannotViewOrCreateExpenses_Returns403()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var frontDeskToken = CreateTestJwt(2, "Receptionist", Quyen.ManageBookings, Quyen.CheckInOut, Quyen.ManageInvoices, Quyen.ManagePayments, Quyen.OrderRoomService);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", frontDeskToken);

        var getResp = await client.GetAsync("api/v1/chi-phi");
        getResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var postResp = await client.PostAsJsonAsync("api/v1/chi-phi", new
        {
            DanhMuc = "Vận hành",
            MoTa = "Chi khống",
            SoTien = 500_000
        });
        postResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SoD_Manager_CannotApproveOwnExpense_Returns400()
    {
        await using var factory = new DacnWebAppFactory();
        int expenseId = 0;
        factory.SeedDatabase(db =>
        {
            var cp = new ChiPhiVanHanh
            {
                DanhMuc = "Bảo trì",
                MoTa = "Sửa điều hòa tầng 3",
                SoTien = 1_500_000m,
                NgayChi = DateOnly.FromDateTime(DateTime.UtcNow),
                NguoiGhiNhan = 5, // Manager (UserId = 5) tự lập phiếu
                TrangThai = "Pending",
                NgayTao = DateTime.UtcNow
            };
            db.ChiPhiVanHanhs.Add(cp);
            db.SaveChanges();
            expenseId = cp.MaId;
        });

        var client = factory.CreateClient();
        var managerToken = CreateTestJwt(5, "Manager", Quyen.ManageExpenses);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        // Manager (UserId = 5) cố tình tự duyệt phiếu chi do chính mình tạo -> Bị chặn bởi nguyên tắc 4 mắt (SoD)
        var resp = await client.PutAsync($"api/v1/chi-phi/{expenseId}/duyet?trangThai=Approved", null);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Bola_Receptionist_CannotUpdateAdminProfile_Returns403()
    {
        await using var factory = new DacnWebAppFactory();
        var client = factory.CreateClient();
        var receptionistToken = CreateTestJwt(2, "Receptionist", Quyen.ManageBookings, Quyen.CheckInOut);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", receptionistToken);

        // Lễ tân (ID = 2) cố tình gửi PUT để sửa hồ sơ của Admin (ID = 1)
        var resp = await client.PutAsJsonAsync("api/v1/nguoi-dung/1", new
        {
            HoTen = "Hacked Admin Name",
            SoDienThoai = "0999999999"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
