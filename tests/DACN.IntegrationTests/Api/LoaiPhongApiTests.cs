using System.Net;
using System.Net.Http.Json;
using DACN.Infrastructure.Data;
using DACN.IntegrationTests.Fixtures;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.Phong;
using FluentAssertions;

namespace DACN.IntegrationTests.Api;

/// <summary>
/// Integration tests cho LoaiPhong API — endpoint AllowAnonymous công khai.
/// </summary>
public class LoaiPhongApiTests : IClassFixture<DacnWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly DacnWebAppFactory _factory;

    public LoaiPhongApiTests(DacnWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─── GET /api/v1/loai-phong ──────────────────────────────────────────────

    [Fact]
    public async Task DanhSach_Returns200_WithoutAuth()
    {
        var response = await _client.GetAsync("api/v1/loai-phong");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DanhSach_Returns200_WithEmptyList_WhenNoData()
    {
        var response = await _client.GetAsync("api/v1/loai-phong");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LoaiPhongDto>>>();
        body.Should().NotBeNull();
        body!.ThanhCong.Should().BeTrue();
        body.DuLieu.Should().NotBeNull();
    }

    [Fact]
    public async Task DanhSach_Returns200_WithSeededData()
    {
        // Tạo factory riêng để tránh conflict data với test khác
        await using var factory = new DacnWebAppFactory();
        factory.SeedDatabase(db =>
        {
            db.LoaiPhongs.Add(new LoaiPhong
            {
                Ten = "Phòng Standard",
                DuongDanRutGon = "phong-standard",
                GiaCoBan = 500_000,
                SoNguoiLonToiDa = 2,
                SoTreEmToiDa = 1,
                DangHoatDong = true
            });
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("api/v1/loai-phong");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LoaiPhongDto>>>();
        body!.DuLieu.Should().HaveCountGreaterThan(0);
    }

    // ─── GET /api/v1/loai-phong/{id} ─────────────────────────────────────────

    [Fact]
    public async Task ChiTiet_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("api/v1/loai-phong/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChiTiet_Returns200_WhenExists()
    {
        await using var factory = new DacnWebAppFactory();
        int seededId = 0;
        factory.SeedDatabase(db =>
        {
            var lp = new LoaiPhong
            {
                Ten = "Phòng Deluxe",
                DuongDanRutGon = "phong-deluxe",
                GiaCoBan = 1_000_000,
                SoNguoiLonToiDa = 2,
                DangHoatDong = true
            };
            db.LoaiPhongs.Add(lp);
            db.SaveChanges();
            seededId = lp.MaId;
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync($"api/v1/loai-phong/{seededId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoaiPhongDto>>();
        body!.DuLieu!.Ten.Should().Be("Phòng Deluxe");
    }

    // ─── POST /api/v1/loai-phong (Authorize) ─────────────────────────────────

    [Fact]
    public async Task Tao_Returns401_WhenNoToken()
    {
        var response = await _client.PostAsJsonAsync("api/v1/loai-phong", new
        {
            Ten = "Phòng Mới",
            GiaCoBan = 500_000,
            SoNguoiLonToiDa = 2
        });

        // Authorize mà không có token → 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── GET /api/v1/loai-phong/tim-kiem ─────────────────────────────────────

    [Fact]
    public async Task TimKiem_Returns400_WhenNgayNhanPhongInPast()
    {
        // Dùng ngày rất xa trong quá khứ để không bị timezone edge case (UTC vs local)
        var pastDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var futureDate = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync(
            $"api/v1/loai-phong/tim-kiem?ngayNhanPhong={pastDate}&ngayTraPhong={futureDate}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TimKiem_Returns400_WhenNgayTraPhongBeforeNgayNhan()
    {
        var date1 = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd");
        var date2 = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd"); // trả trước nhận

        var response = await _client.GetAsync(
            $"api/v1/loai-phong/tim-kiem?ngayNhanPhong={date1}&ngayTraPhong={date2}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
