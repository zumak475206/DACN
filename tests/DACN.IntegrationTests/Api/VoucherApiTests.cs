using System.Net;
using System.Net.Http.Json;
using DACN.Infrastructure.Data;
using DACN.IntegrationTests.Fixtures;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.Voucher;
using FluentAssertions;

namespace DACN.IntegrationTests.Api;

/// <summary>
/// Integration tests cho Voucher API — đặc biệt endpoint AllowAnonymous /kiem-tra.
/// </summary>
public class VoucherApiTests : IClassFixture<DacnWebAppFactory>
{
    private readonly HttpClient _client;

    public VoucherApiTests(DacnWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── POST /api/v1/voucher/kiem-tra ────────────────────────────────────────

    [Fact]
    public async Task KiemTra_Returns200_WhenVoucherNotFound()
    {
        var response = await _client.PostAsJsonAsync("api/v1/voucher/kiem-tra", new
        {
            MaCode = "KHONG_TON_TAI",
            TongTienDatPhong = 1_000_000
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<KiemTraVoucherResponse>>();
        body!.ThanhCong.Should().BeTrue();      // API luôn 200
        body.DuLieu!.HopLe.Should().BeFalse();  // nhưng HopLe = false
    }

    [Fact]
    public async Task KiemTra_WithValidActiveVoucher_Returns200_AndHopLeTrue()
    {
        // Seed voucher hợp lệ
        await using var factory = new DacnWebAppFactory();
        factory.SeedDatabase(db =>
        {
            db.MaGiamGia.Add(new MaGiamGium
            {
                MaCode = "TEST10",
                LoaiGiamGia = "phan_tram",
                GiaTriGiamGia = 10,
                GiaTriDatPhongToiThieu = null,
                HieuLucTu = DateTime.Now.AddDays(-1),
                HieuLucDen = DateTime.Now.AddDays(30),
                GioiHanSuDung = 100,
                SoLanDaDung = 0,
                DangHoatDong = true,
                NgayTao = DateTime.UtcNow
            });
        });

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/v1/voucher/kiem-tra", new
        {
            MaCode = "TEST10",
            TongTienDatPhong = 1_000_000
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<KiemTraVoucherResponse>>();
        body!.DuLieu!.HopLe.Should().BeTrue();
        body.DuLieu.TienGiam.Should().Be(100_000); // 10% của 1_000_000
    }

    [Fact]
    public async Task KiemTra_Returns200_HopLeFalse_WhenTongTienBelowMinimum()
    {
        await using var factory = new DacnWebAppFactory();
        factory.SeedDatabase(db =>
        {
            db.MaGiamGia.Add(new MaGiamGium
            {
                MaCode = "MINTEST",
                LoaiGiamGia = "co_dinh",
                GiaTriGiamGia = 100_000,
                GiaTriDatPhongToiThieu = 2_000_000, // yêu cầu tối thiểu 2tr
                HieuLucTu = DateTime.Now.AddDays(-1),
                HieuLucDen = DateTime.Now.AddDays(30),
                GioiHanSuDung = 50,
                SoLanDaDung = 0,
                DangHoatDong = true,
                NgayTao = DateTime.UtcNow
            });
        });

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/v1/voucher/kiem-tra", new
        {
            MaCode = "MINTEST",
            TongTienDatPhong = 500_000 // dưới tối thiểu
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<KiemTraVoucherResponse>>();
        body!.DuLieu!.HopLe.Should().BeFalse();
    }

    // ─── GET /api/v1/voucher (Authorize + IsStaffOrAdmin) ────────────────────

    [Fact]
    public async Task DanhSach_Returns401_WhenNoToken()
    {
        var response = await _client.GetAsync("api/v1/voucher");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
