using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DACN.Infrastructure.Data;
using DACN.Web.DTOs.Auth;
using DACN.Web.DTOs.Common;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DACN.Web.Controllers;

[Route("api/v1/auth")]
[ApiController]
[EnableRateLimiting("auth-policy")]
public class AuthController(DACNContext db, IConfiguration config, IAuditService auditService) : ControllerBase
{
    // Lưu trữ tạm mã OTP đặt lại mật khẩu trong bộ nhớ (phục vụ đồ án không cần server email bên ngoài)
    private static readonly Dictionary<string, (string Otp, DateTime HetHan, int SoLanSai)> _resetOtpCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _otpLock = new();

    [HttpPost("dang-nhap")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DangNhapResponse>>> DangNhap([FromBody] DangNhapRequest req)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var user = await db.NguoiDungs
            .Include(u => u.MaVaiTroNavigation)
                .ThenInclude(v => v.MaQuyenHans)
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.TrangThai);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.MatKhau, user.MaBamMatKhau))
        {
            await auditService.GhiNhanAsync(
                hanhDong: "DANG_NHAP_THAT_BAI",
                doiTuong: "Auth",
                email: req.Email,
                noiDung: "Thử đăng nhập thất bại do sai email/mật khẩu hoặc tài khoản đã bị khóa",
                diaChiIp: clientIp,
                thanhCong: false);

            return Unauthorized(ApiResponse<DangNhapResponse>.Fail("Email hoặc mật khẩu không đúng."));
        }

        var token = TaoJwtToken(user, out var expiresAt);
        var refreshToken = await TaoVaLuuRefreshTokenAsync(user.MaId, clientIp);

        await auditService.GhiNhanAsync(
            hanhDong: "DANG_NHAP_THANH_CONG",
            doiTuong: "Auth",
            khoaChinh: user.MaId.ToString(),
            maNguoiDung: user.MaId,
            email: user.Email,
            noiDung: $"Đăng nhập thành công với vai trò '{user.MaVaiTroNavigation?.Ten}'",
            diaChiIp: clientIp,
            thanhCong: true);

        return Ok(ApiResponse<DangNhapResponse>.Ok(new DangNhapResponse
        {
            MaId = user.MaId,
            HoTen = user.HoTen,
            Email = user.Email,
            SoDienThoai = user.SoDienThoai,
            LoaiNguoiDung = user.LoaiNguoiDung,
            MaVaiTro = user.MaVaiTro,
            TenVaiTro = user.MaVaiTroNavigation?.Ten ?? "Khách hàng",
            TrangThai = user.TrangThai,
            DanhSachQuyen = user.MaVaiTroNavigation?.MaQuyenHans.Select(q => q.Ten).ToList() ?? new(),
            TongDiem = user.TongDiem,
            Token = token,
            LoaiToken = "Bearer",
            HetHanVao = expiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenHetHanVao = refreshToken.HetHanVao
        }, "Đăng nhập thành công!"));
    }

    [HttpPost("dang-ky")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DangNhapResponse>>> DangKy([FromBody] DangKyRequest req)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (await db.NguoiDungs.AnyAsync(u => u.Email == req.Email))
            return BadRequest(ApiResponse<DangNhapResponse>.Fail("Email đã được sử dụng."));

        var vaiTroKhach = await db.VaiTros.FirstOrDefaultAsync(v => v.Ten.ToLower().Contains("khach") || v.Ten.ToLower().Contains("customer"))
                          ?? await db.VaiTros.OrderBy(v => v.MaId).FirstOrDefaultAsync();

        if (vaiTroKhach == null)
        {
            vaiTroKhach = new VaiTro { Ten = "Khách hàng", MoTa = "Khách hàng thông thường" };
            db.VaiTros.Add(vaiTroKhach);
            await db.SaveChangesAsync();
        }

        var user = new NguoiDung
        {
            HoTen = req.HoTen,
            Email = req.Email,
            SoDienThoai = req.SoDienThoai,
            MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword(req.MatKhau),
            LoaiNguoiDung = "customer",
            MaVaiTro = vaiTroKhach.MaId,
            TrangThai = true,
            TongDiem = 0,
            NgayTao = DateTime.UtcNow
        };

        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        user.MaVaiTroNavigation = vaiTroKhach;
        var token = TaoJwtToken(user, out var expiresAt);
        var refreshToken = await TaoVaLuuRefreshTokenAsync(user.MaId, clientIp);

        await auditService.GhiNhanAsync(
            hanhDong: "DANG_KY",
            doiTuong: "Auth",
            khoaChinh: user.MaId.ToString(),
            maNguoiDung: user.MaId,
            email: user.Email,
            noiDung: $"Đăng ký tài khoản khách hàng mới '{user.HoTen}'",
            diaChiIp: clientIp,
            thanhCong: true);

        return Ok(ApiResponse<DangNhapResponse>.Ok(new DangNhapResponse
        {
            MaId = user.MaId,
            HoTen = user.HoTen,
            Email = user.Email,
            SoDienThoai = user.SoDienThoai,
            LoaiNguoiDung = user.LoaiNguoiDung,
            TenVaiTro = vaiTroKhach.Ten,
            TongDiem = 0,
            Token = token,
            LoaiToken = "Bearer",
            HetHanVao = expiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenHetHanVao = refreshToken.HetHanVao
        }, "Đăng ký thành công!"));
    }

    [HttpPost("lam-moi-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DangNhapResponse>>> LamMoiToken([FromBody] LamMoiTokenRequest req)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var existingToken = await db.RefreshTokens
            .Include(t => t.MaNguoiDungNavigation)
            .ThenInclude(u => u.MaVaiTroNavigation)
                .ThenInclude(v => v.MaQuyenHans)
            .FirstOrDefaultAsync(t => t.Token == req.RefreshToken);

        if (existingToken == null)
            return Unauthorized(ApiResponse<DangNhapResponse>.Fail("Refresh token không tồn tại."));

        // Nếu token đã bị thu hồi nhưng vẫn cố dùng -> Rủi ro bảo mật (Token replay attack)
        if (existingToken.DaThuHoi)
        {
            await auditService.GhiNhanAsync(
                hanhDong: "CANH_BAO_BAO_MAT",
                doiTuong: "Auth",
                maNguoiDung: existingToken.MaNguoiDung,
                noiDung: "Phát hiện cố gắng tái sử dụng Refresh Token đã bị thu hồi!",
                diaChiIp: clientIp,
                thanhCong: false);

            return Unauthorized(ApiResponse<DangNhapResponse>.Fail("Token không hợp lệ hoặc đã bị vô hiệu hóa vì lý do bảo mật."));
        }

        if (existingToken.DaHetHan)
            return Unauthorized(ApiResponse<DangNhapResponse>.Fail("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."));

        var user = existingToken.MaNguoiDungNavigation;
        if (!user.TrangThai)
            return Unauthorized(ApiResponse<DangNhapResponse>.Fail("Tài khoản của bạn đã bị vô hiệu hóa."));

        // Thu hồi token cũ (Token Rotation)
        existingToken.DaThuHoi = true;
        existingToken.NgayThuHoi = DateTime.UtcNow;
        existingToken.LyDoThuHoi = "Làm mới token";

        // Cấp Refresh Token mới
        var newRefreshToken = TaoRefreshTokenEntity(user.MaId, clientIp);
        existingToken.ThayTheBoiToken = newRefreshToken.Token;

        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync();

        // Cấp Access Token JWT mới
        var newJwtToken = TaoJwtToken(user, out var expiresAt);

        return Ok(ApiResponse<DangNhapResponse>.Ok(new DangNhapResponse
        {
            MaId = user.MaId,
            HoTen = user.HoTen,
            Email = user.Email,
            SoDienThoai = user.SoDienThoai,
            LoaiNguoiDung = user.LoaiNguoiDung,
            TenVaiTro = user.MaVaiTroNavigation?.Ten ?? "Khách hàng",
            TongDiem = user.TongDiem,
            Token = newJwtToken,
            LoaiToken = "Bearer",
            HetHanVao = expiresAt,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenHetHanVao = newRefreshToken.HetHanVao
        }, "Làm mới token thành công."));
    }

    [HttpPost("dang-xuat")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> DangXuat([FromBody] DangXuatRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.RefreshToken))
        {
            var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == req.RefreshToken);
            if (token != null && !token.DaThuHoi)
            {
                token.DaThuHoi = true;
                token.NgayThuHoi = DateTime.UtcNow;
                token.LyDoThuHoi = "Người dùng chủ động đăng xuất";
                await db.SaveChangesAsync();
            }
        }

        return Ok(ApiResponse<object>.Ok(null!, "Đăng xuất thành công."));
    }

    [HttpPost("quen-mat-khau")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> QuenMatKhau([FromBody] QuenMatKhauRequest req)
    {
        var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == req.Email && u.TrangThai);
        if (user == null)
        {
            // Bảo mật: không báo lộ ra email có tồn tại hay không
            return Ok(ApiResponse<object>.Ok(null!, "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã xác nhận đặt lại mật khẩu."));
        }

        // Tạo mã OTP ngẫu nhiên 6 chữ số có hiệu lực trong 15 phút
        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        lock (_otpLock)
        {
            _resetOtpCache[user.Email] = (otp, DateTime.UtcNow.AddMinutes(15), 0);
        }

        await auditService.GhiNhanAsync(
            hanhDong: "YEU_CAU_QUEN_MAT_KHAU",
            doiTuong: "Auth",
            khoaChinh: user.MaId.ToString(),
            maNguoiDung: user.MaId,
            email: user.Email,
            noiDung: $"Yêu cầu mã đặt lại mật khẩu (OTP Demo: {otp})",
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        // Trả về kèm gợi ý mã OTP trong môi trường đồ án (để dễ dàng test mà không cần mail server thật)
        return Ok(ApiResponse<object>.Ok(new { MaXacNhanDemo = otp, HetHanSauPhut = 15 },
            "Mã xác nhận đã được tạo thành công. Vui lòng sử dụng mã để đặt lại mật khẩu."));
    }

    [HttpPost("dat-lai-mat-khau")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> DatLaiMatKhau([FromBody] DatLaiMatKhauRequest req)
    {
        var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == req.Email && u.TrangThai);
        if (user == null)
            return BadRequest(ApiResponse<object>.Fail("Yêu cầu không hợp lệ."));

        bool otpValid = false;
        lock (_otpLock)
        {
            if (_resetOtpCache.TryGetValue(req.Email, out var entry))
            {
                if (DateTime.UtcNow > entry.HetHan)
                {
                    _resetOtpCache.Remove(req.Email);
                }
                else if (entry.Otp == req.MaXacNhan)
                {
                    otpValid = true;
                    _resetOtpCache.Remove(req.Email);
                }
                else
                {
                    var nextFail = entry.SoLanSai + 1;
                    if (nextFail >= 5)
                        _resetOtpCache.Remove(req.Email);
                    else
                        _resetOtpCache[req.Email] = (entry.Otp, entry.HetHan, nextFail);
                }
            }
        }

        if (!otpValid)
            return BadRequest(ApiResponse<object>.Fail("Mã xác nhận không chính xác hoặc đã hết hạn."));

        user.MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword(req.MatKhauMoi);

        // Thu hồi toàn bộ RefreshToken cũ của người dùng này để bắt đăng nhập lại
        var activeTokens = await db.RefreshTokens
            .Where(t => t.MaNguoiDung == user.MaId && !t.DaThuHoi)
            .ToListAsync();
        foreach (var t in activeTokens)
        {
            t.DaThuHoi = true;
            t.NgayThuHoi = DateTime.UtcNow;
            t.LyDoThuHoi = "Đổi mật khẩu thành công";
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "DAT_LAI_MAT_KHAU_THANH_CONG",
            doiTuong: "Auth",
            khoaChinh: user.MaId.ToString(),
            maNguoiDung: user.MaId,
            email: user.Email,
            noiDung: "Người dùng đã đặt lại mật khẩu thành công qua mã OTP xác nhận",
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<DangNhapResponse>>> GetProfileHienTai()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(ApiResponse<DangNhapResponse>.Fail("Không xác định được danh tính người dùng."));

        var user = await db.NguoiDungs
            .Include(u => u.MaVaiTroNavigation)
                .ThenInclude(v => v.MaQuyenHans)
            .FirstOrDefaultAsync(u => u.MaId == userId && u.TrangThai);

        if (user == null) return NotFound(ApiResponse<DangNhapResponse>.Fail("Không tìm thấy người dùng."));

        return Ok(ApiResponse<DangNhapResponse>.Ok(new DangNhapResponse
        {
            MaId = user.MaId,
            HoTen = user.HoTen,
            Email = user.Email,
            SoDienThoai = user.SoDienThoai,
            LoaiNguoiDung = user.LoaiNguoiDung,
            MaVaiTro = user.MaVaiTro,
            TenVaiTro = user.MaVaiTroNavigation?.Ten ?? "Khách hàng",
            TrangThai = user.TrangThai,
            DanhSachQuyen = user.MaVaiTroNavigation?.MaQuyenHans.Select(q => q.Ten).ToList() ?? new(),
            TongDiem = user.TongDiem
        }));
    }

    [Authorize]
    [HttpPost("doi-mat-khau")]
    public async Task<ActionResult<ApiResponse<object>>> DoiMatKhau([FromBody] DoiMatKhauRequest req)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            return Unauthorized(ApiResponse<object>.Fail("Không xác định được người dùng."));

        var user = await db.NguoiDungs.FindAsync(currentUserId);
        if (user == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));
        if (!BCrypt.Net.BCrypt.Verify(req.MatKhauCu, user.MaBamMatKhau))
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu cũ không đúng."));

        user.MaBamMatKhau = BCrypt.Net.BCrypt.HashPassword(req.MatKhauMoi);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "DOI_MAT_KHAU",
            doiTuong: "Auth",
            khoaChinh: user.MaId.ToString(),
            maNguoiDung: user.MaId,
            email: user.Email,
            noiDung: "Người dùng đổi mật khẩu thành công",
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đổi mật khẩu thành công."));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private string TaoJwtToken(NguoiDung user, out DateTime expiresAt)
    {
        var jwtKey = Environment.GetEnvironmentVariable("DACN_JWT_SECRET")
            ?? Environment.GetEnvironmentVariable("Jwt__Key")
            ?? config["Jwt:Key"]
            ?? "DACN_Hotel_Management_Super_Secret_Key_2026_@_DeepMind_Security";

        var jwtIssuer = config["Jwt:Issuer"] ?? "DACN_HotelManagement";
        var jwtAudience = config["Jwt:Audience"] ?? "DACN_HotelManagement_Clients";
        var expireDays = int.TryParse(config["Jwt:ExpireDays"], out var days) ? days : 7;

        expiresAt = DateTime.UtcNow.AddDays(expireDays);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.MaId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Role, user.MaVaiTroNavigation?.Ten ?? "Khách hàng"),
            new("LoaiNguoiDung", user.LoaiNguoiDung),
            new("loai_nguoi_dung", user.LoaiNguoiDung),
            new("MaVaiTro", user.MaVaiTro.ToString())
        };

        if (user.MaVaiTroNavigation?.MaQuyenHans != null)
        {
            foreach (var q in user.MaVaiTroNavigation.MaQuyenHans)
            {
                claims.Add(new Claim("permission", q.Ten));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static RefreshToken TaoRefreshTokenEntity(int maNguoiDung, string? clientIp)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var tokenString = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return new RefreshToken
        {
            MaNguoiDung = maNguoiDung,
            Token = tokenString,
            HetHanVao = DateTime.UtcNow.AddDays(14), // Refresh token có hiệu lực 14 ngày
            NgayTao = DateTime.UtcNow,
            DiaChiIpTao = clientIp,
            DaThuHoi = false
        };
    }

    private async Task<RefreshToken> TaoVaLuuRefreshTokenAsync(int maNguoiDung, string? clientIp)
    {
        var refreshToken = TaoRefreshTokenEntity(maNguoiDung, clientIp);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();
        return refreshToken;
    }
}
