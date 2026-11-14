using System.Net;
using System.Net.Http.Json;
using DACN.IntegrationTests.Fixtures;
using FluentAssertions;

namespace DACN.IntegrationTests.Api;

/// <summary>
/// Integration tests cho DatPhong API — kiểm tra validation logic.
/// Không test transaction Serializable (InMemory không hỗ trợ).
/// </summary>
public class DatPhongApiTests : IClassFixture<DacnWebAppFactory>
{
    private readonly HttpClient _client;

    public DatPhongApiTests(DacnWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── GET /api/v1/dat-phong (Authorize) ───────────────────────────────────

    [Fact]
    public async Task DanhSach_Returns401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("api/v1/dat-phong");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CuaToi_Returns401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("api/v1/dat-phong/cua-toi");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChiTiet_Returns401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("api/v1/dat-phong/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── POST /api/v1/dat-phong (AllowAnonymous) ─────────────────────────────

    [Fact]
    public async Task TaoDatPhong_Returns400_WhenDanhSachPhongEmpty()
    {
        // TaoDatPhong là AllowAnonymous — không cần token
        // Body hợp lệ nhưng DanhSachPhong rỗng → 400
        var response = await _client.PostAsJsonAsync("api/v1/dat-phong", new
        {
            DanhSachPhong = Array.Empty<object>(),
            GhiChu = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TaoDatPhong_Returns400_WhenNgayNhanPhongInPast()
    {
        // TaoDatPhong AllowAnonymous — gửi ngày quá khứ → 400
        var response = await _client.PostAsJsonAsync("api/v1/dat-phong", new
        {
            DanhSachPhong = new[]
            {
                new
                {
                    MaLoaiPhong = 1,
                    NgayNhanPhong = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd"),
                    NgayTraPhong = DateTime.UtcNow.AddDays(-8).ToString("yyyy-MM-dd")
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── Các route pattern (Authorize) ────────────────────────────────────────

    [Theory]
    [InlineData("api/v1/dat-phong")]
    [InlineData("api/v1/dat-phong/cua-toi")]
    [InlineData("api/v1/dat-phong/99")]
    public async Task ProtectedRoutes_Returns401_WhenNoToken(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
