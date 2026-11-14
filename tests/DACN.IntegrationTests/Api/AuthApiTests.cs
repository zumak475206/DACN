using System.Net;
using System.Net.Http.Json;
using DACN.Infrastructure.Data;
using DACN.IntegrationTests.Fixtures;
using DACN.Web.DTOs.Auth;
using DACN.Web.DTOs.Common;
using FluentAssertions;

namespace DACN.IntegrationTests.Api;

/// <summary>
/// Integration tests cho Auth API endpoints.
/// Dùng InMemory DB — không cần SQL Server thực.
/// </summary>
public class AuthApiTests : IClassFixture<DacnWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly DacnWebAppFactory _factory;

    public AuthApiTests(DacnWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─── POST /api/v1/auth/dang-nhap ─────────────────────────────────────────

    [Fact]
    public async Task DangNhap_Returns400_WhenMissingEmail()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-nhap", new
        {
            MatKhau = "password123"
            // Email bị bỏ
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DangNhap_Returns400_WhenMissingPassword()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-nhap", new
        {
            Email = "test@example.com"
            // MatKhau bị bỏ
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DangNhap_Returns400_WhenInvalidEmailFormat()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-nhap", new
        {
            Email = "not-an-email",
            MatKhau = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DangNhap_Returns401_WhenWrongCredentials()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-nhap", new
        {
            Email = "nonexistent@example.com",
            MatKhau = "wrongpassword"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── POST /api/v1/auth/dang-ky ───────────────────────────────────────────

    [Fact]
    public async Task DangKy_Returns400_WhenMissingHoTen()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-ky", new
        {
            Email = "newuser@example.com",
            MatKhau = "password123"
            // HoTen bị bỏ
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DangKy_Returns400_WhenInvalidEmail()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-ky", new
        {
            HoTen = "Nguyễn Văn A",
            Email = "invalid-email",
            MatKhau = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DangKy_Returns400_WhenPasswordTooShort()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-ky", new
        {
            HoTen = "Nguyễn Văn A",
            Email = "valid@example.com",
            MatKhau = "123" // dưới 6 ký tự
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DangKy_Returns400_WhenEmailDuplicate()
    {
        // Seed user với email đã tồn tại
        _factory.SeedDatabase(db =>
        {
            db.NguoiDungs.Add(new NguoiDung
            {
                HoTen = "Existing User",
                Email = "duplicate@example.com",
                MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword("password123"),
                LoaiNguoiDung = "customer",
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            });
        });

        var response = await _client.PostAsJsonAsync("api/v1/auth/dang-ky", new
        {
            HoTen = "Người Dùng Mới",
            Email = "duplicate@example.com",
            MatKhau = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DangNhapResponse>>();
        body!.ThanhCong.Should().BeFalse();
        body.ThongBao.Should().Contain("Email");
    }

    // ─── GET /api/v1/auth/me ─────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_Returns401_WhenNoToken()
    {
        // Route thực tế là /api/v1/auth/me
        var response = await _client.GetAsync("api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_Returns401_WhenInvalidToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/me");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Refresh Token & Logout ──────────────────────────────────────────────

    [Fact]
    public async Task DangNhap_ReturnsRefreshToken_WhenSuccess()
    {
        await using var factory = new DacnWebAppFactory();
        factory.SeedDatabase(db =>
        {
            var role = new VaiTro { Ten = "Customer", MoTa = "Khách hàng" };
            db.VaiTros.Add(role);
            db.SaveChanges();

            db.NguoiDungs.Add(new NguoiDung
            {
                HoTen = "Login User",
                Email = "login_success@example.com",
                MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword("validpass123"),
                LoaiNguoiDung = "customer",
                MaVaiTro = role.MaId,
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            });
        });

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/v1/auth/dang-nhap", new
        {
            Email = "login_success@example.com",
            MatKhau = "validpass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DangNhapResponse>>();
        body!.ThanhCong.Should().BeTrue();
        body.DuLieu!.Token.Should().NotBeNullOrEmpty();
        body.DuLieu.RefreshToken.Should().NotBeNullOrEmpty();
        body.DuLieu.RefreshTokenHetHanVao.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LamMoiToken_Returns200_AndRotatesTokens_WhenValid()
    {
        await using var factory = new DacnWebAppFactory();
        string initialRefreshToken = "";
        factory.SeedDatabase(db =>
        {
            var role = new VaiTro { Ten = "Customer", MoTa = "Khách hàng" };
            db.VaiTros.Add(role);
            db.SaveChanges();

            var user = new NguoiDung
            {
                HoTen = "Refresh User",
                Email = "refresh_user@example.com",
                MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword("validpass123"),
                LoaiNguoiDung = "customer",
                MaVaiTro = role.MaId,
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            };
            db.NguoiDungs.Add(user);
            db.SaveChanges();

            var token = new RefreshToken
            {
                MaNguoiDung = user.MaId,
                Token = "valid-refresh-token-12345",
                HetHanVao = DateTime.UtcNow.AddDays(7),
                NgayTao = DateTime.UtcNow,
                DaThuHoi = false
            };
            db.RefreshTokens.Add(token);
            initialRefreshToken = token.Token;
        });

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/v1/auth/lam-moi-token", new
        {
            RefreshToken = initialRefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DangNhapResponse>>();
        body!.ThanhCong.Should().BeTrue();
        body.DuLieu!.Token.Should().NotBeNullOrEmpty();
        body.DuLieu.RefreshToken.Should().NotBeNullOrEmpty();
        body.DuLieu.RefreshToken.Should().NotBe(initialRefreshToken); // Token rotated!
    }

    [Fact]
    public async Task LamMoiToken_Returns401_WhenTokenNotFound()
    {
        var response = await _client.PostAsJsonAsync("api/v1/auth/lam-moi-token", new
        {
            RefreshToken = "non-existent-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LamMoiToken_Returns401_WhenTokenAlreadyRevoked()
    {
        await using var factory = new DacnWebAppFactory();
        string revokedToken = "revoked-token-abc";
        factory.SeedDatabase(db =>
        {
            var role = new VaiTro { Ten = "Customer", MoTa = "Khách hàng" };
            db.VaiTros.Add(role);
            db.SaveChanges();

            var user = new NguoiDung
            {
                HoTen = "Revoked User",
                Email = "revoked_user@example.com",
                MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword("validpass123"),
                LoaiNguoiDung = "customer",
                MaVaiTro = role.MaId,
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            };
            db.NguoiDungs.Add(user);
            db.SaveChanges();

            db.RefreshTokens.Add(new RefreshToken
            {
                MaNguoiDung = user.MaId,
                Token = revokedToken,
                HetHanVao = DateTime.UtcNow.AddDays(7),
                NgayTao = DateTime.UtcNow,
                DaThuHoi = true,
                NgayThuHoi = DateTime.UtcNow
            });
        });

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/v1/auth/lam-moi-token", new
        {
            RefreshToken = revokedToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DangXuat_RevokesRefreshToken()
    {
        await using var factory = new DacnWebAppFactory();
        string tokenToRevoke = "token-to-logout-xyz";
        factory.SeedDatabase(db =>
        {
            var role = new VaiTro { Ten = "Customer", MoTa = "Khách hàng" };
            db.VaiTros.Add(role);
            db.SaveChanges();

            var user = new NguoiDung
            {
                HoTen = "Logout User",
                Email = "logout_user@example.com",
                MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword("validpass123"),
                LoaiNguoiDung = "customer",
                MaVaiTro = role.MaId,
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            };
            db.NguoiDungs.Add(user);
            db.SaveChanges();

            db.RefreshTokens.Add(new RefreshToken
            {
                MaNguoiDung = user.MaId,
                Token = tokenToRevoke,
                HetHanVao = DateTime.UtcNow.AddDays(7),
                NgayTao = DateTime.UtcNow,
                DaThuHoi = false
            });
        });

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/v1/auth/dang-xuat", new
        {
            RefreshToken = tokenToRevoke
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Sau khi đăng xuất, cố dùng refresh token đó phải bị 401
        var refreshResp = await client.PostAsJsonAsync("api/v1/auth/lam-moi-token", new
        {
            RefreshToken = tokenToRevoke
        });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Quên & Đặt lại mật khẩu ─────────────────────────────────────────────

    [Fact]
    public async Task QuenMatKhau_And_DatLaiMatKhau_SuccessFlow()
    {
        await using var factory = new DacnWebAppFactory();
        string email = "forgot_test@example.com";
        factory.SeedDatabase(db =>
        {
            var role = new VaiTro { Ten = "Customer", MoTa = "Khách hàng" };
            db.VaiTros.Add(role);
            db.SaveChanges();

            db.NguoiDungs.Add(new NguoiDung
            {
                HoTen = "Forgot Password User",
                Email = email,
                MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword("oldpassword123"),
                LoaiNguoiDung = "customer",
                MaVaiTro = role.MaId,
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            });
        });

        var client = factory.CreateClient();

        // 1. Yêu cầu quên mật khẩu
        var quenResp = await client.PostAsJsonAsync("api/v1/auth/quen-mat-khau", new
        {
            Email = email
        });
        quenResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var quenBody = await quenResp.Content.ReadFromJsonAsync<ApiResponse<System.Text.Json.JsonElement>>();
        quenBody!.ThanhCong.Should().BeTrue();
        var otp = quenBody.DuLieu.GetProperty("maXacNhanDemo").GetString();
        otp.Should().NotBeNullOrEmpty();

        // 2. Đặt lại mật khẩu với OTP vừa nhận
        var datLaiResp = await client.PostAsJsonAsync("api/v1/auth/dat-lai-mat-khau", new
        {
            Email = email,
            MaXacNhan = otp,
            MatKhauMoi = "newpassword456"
        });
        datLaiResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Đăng nhập lại với mật khẩu mới phải thành công
        var loginResp = await client.PostAsJsonAsync("api/v1/auth/dang-nhap", new
        {
            Email = email,
            MatKhau = "newpassword456"
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
