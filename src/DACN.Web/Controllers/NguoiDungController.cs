using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Auth;
using DACN.Web.DTOs.Common;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Authorize]
[Route("api/v1/nguoi-dung")]
[ApiController]
public class NguoiDungController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<KetQuaPhanTrang<DangNhapResponse>>>> DanhSach(
        [FromQuery] PhanTrangRequest req,
        [FromQuery] string? tuKhoa = null,
        [FromQuery] int? maVaiTro = null,
        [FromQuery] bool? trangThai = null,
        [FromQuery] string? loaiNguoiDung = null)
    {
        if (!User.HasPermission(Quyen.ManageUsers) && !User.IsManagerOrAdmin())
            return Forbid();

        var query = db.NguoiDungs.AsNoTracking().Include(u => u.MaVaiTroNavigation).AsQueryable();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var kw = tuKhoa.Trim().ToLower();
            query = query.Where(u =>
                u.HoTen.ToLower().Contains(kw) ||
                u.Email.ToLower().Contains(kw) ||
                (u.SoDienThoai != null && u.SoDienThoai.Contains(kw)));
        }

        if (maVaiTro.HasValue)
            query = query.Where(u => u.MaVaiTro == maVaiTro.Value);

        if (trangThai.HasValue)
            query = query.Where(u => u.TrangThai == trangThai.Value);

        if (!string.IsNullOrWhiteSpace(loaiNguoiDung))
        {
            var normalizedLoai = StatusMapper.NormalizeLoaiNguoiDung(loaiNguoiDung);
            query = query.Where(u => u.LoaiNguoiDung == normalizedLoai);
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(u => u.NgayTao)
            .Skip((req.Trang - 1) * req.SoLuong)
            .Take(req.SoLuong)
            .Select(u => new DangNhapResponse
            {
                MaId = u.MaId, HoTen = u.HoTen, Email = u.Email,
                SoDienThoai = u.SoDienThoai, LoaiNguoiDung = u.LoaiNguoiDung,
                MaVaiTro = u.MaVaiTro,
                TenVaiTro = u.MaVaiTroNavigation != null ? u.MaVaiTroNavigation.Ten : "Khách hàng",
                TrangThai = u.TrangThai,
                TongDiem = u.TongDiem
            })
            .ToListAsync();

        return Ok(ApiResponse<KetQuaPhanTrang<DangNhapResponse>>.Ok(
            new KetQuaPhanTrang<DangNhapResponse> { DuLieu = data, TongSo = total, Trang = req.Trang, SoLuong = req.SoLuong }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DangNhapResponse>>> ChiTiet(int id)
    {
        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && id != currentUserId)
            return Forbid();

        var u = await db.NguoiDungs.AsNoTracking().Include(x => x.MaVaiTroNavigation).FirstOrDefaultAsync(x => x.MaId == id);
        if (u == null) return NotFound(ApiResponse<DangNhapResponse>.Fail("Không tìm thấy người dùng."));

        return Ok(ApiResponse<DangNhapResponse>.Ok(new DangNhapResponse
        {
            MaId = u.MaId, HoTen = u.HoTen, Email = u.Email,
            SoDienThoai = u.SoDienThoai, LoaiNguoiDung = u.LoaiNguoiDung,
            MaVaiTro = u.MaVaiTro,
            TenVaiTro = u.MaVaiTroNavigation?.Ten ?? "Khách hàng",
            TrangThai = u.TrangThai,
            TongDiem = u.TongDiem
        }));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhat(int id, [FromBody] CapNhatHoSoRequest req)
    {
        var currentUserId = User.GetUserId();
        if (!User.HasPermission(Quyen.ManageUsers) && id != currentUserId)
            return Forbid();

        var u = await db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

        u.HoTen = req.HoTen;
        u.SoDienThoai = req.SoDienThoai;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật hồ sơ thành công."));
    }

    [HttpPut("{id:int}/kich-hoat")]
    public async Task<ActionResult<ApiResponse<object>>> KichHoat(int id, [FromQuery] bool trangThai)
    {
        if (!User.HasPermission(Quyen.ManageUsers))
            return Forbid();

        var currentUserId = User.GetUserId();
        if (id == currentUserId && !trangThai)
            return BadRequest(ApiResponse<object>.Fail("Không thể tự vô hiệu hóa tài khoản của chính mình."));

        var u = await db.NguoiDungs.Include(x => x.MaVaiTroNavigation).FirstOrDefaultAsync(x => x.MaId == id);
        if (u == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

        var isTargetAdmin = u.MaVaiTroNavigation?.Ten.ToLowerInvariant().Contains("admin") == true;
        if (isTargetAdmin && !User.IsAdmin())
            return Forbid();

        if (isTargetAdmin && !trangThai)
        {
            var activeAdminCount = await db.NguoiDungs
                .Include(x => x.MaVaiTroNavigation)
                .CountAsync(x => x.TrangThai && x.MaId != id && x.MaVaiTroNavigation != null && x.MaVaiTroNavigation.Ten.ToLower().Contains("admin"));
            if (activeAdminCount == 0)
                return BadRequest(ApiResponse<object>.Fail("Không thể vô hiệu hóa Quản trị viên hoạt động cuối cùng của hệ thống."));
        }

        u.TrangThai = trangThai;

        if (!trangThai)
        {
            var activeTokens = await db.RefreshTokens
                .Where(t => t.MaNguoiDung == u.MaId && !t.DaThuHoi)
                .ToListAsync();
            foreach (var token in activeTokens)
            {
                token.DaThuHoi = true;
                token.NgayThuHoi = DateTime.UtcNow;
                token.LyDoThuHoi = "Tài khoản bị vô hiệu hóa";
            }
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: trangThai ? "KICH_HOAT_TAI_KHOAN" : "VO_HIEU_HOA_TAI_KHOAN",
            doiTuong: "NguoiDung",
            khoaChinh: u.MaId.ToString(),
            noiDung: $"{(trangThai ? "Kích hoạt" : "Vô hiệu hóa")} tài khoản của người dùng '{u.HoTen}' ({u.Email})",
            maNguoiDung: currentUserId,
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, trangThai ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản và thu hồi phiên đăng nhập."));
    }

    [HttpPut("{id:int}/vai-tro")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatVaiTro(int id, [FromBody] CapNhatVaiTroNguoiDungRequest req)
    {
        if (!User.HasPermission(Quyen.ManageUsers))
            return Forbid();

        var currentUserId = User.GetUserId();
        if (id == currentUserId)
            return BadRequest(ApiResponse<object>.Fail("Không thể tự thay đổi vai trò của chính mình."));

        var u = await db.NguoiDungs.Include(x => x.MaVaiTroNavigation).FirstOrDefaultAsync(x => x.MaId == id);
        if (u == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

        var vaiTro = await db.VaiTros.FindAsync(req.MaVaiTro);
        if (vaiTro == null) return BadRequest(ApiResponse<object>.Fail("Vai trò không tồn tại."));

        var isNewRoleAdmin = vaiTro.Ten.ToLowerInvariant().Contains("admin");
        var isCurrentTargetAdmin = u.MaVaiTroNavigation?.Ten.ToLowerInvariant().Contains("admin") == true;

        // Chỉ Admin mới được cấp hoặc thu hồi vai trò Admin
        if ((isNewRoleAdmin || isCurrentTargetAdmin) && !User.IsAdmin())
            return Forbid();

        if (isCurrentTargetAdmin && !isNewRoleAdmin)
        {
            var otherAdmins = await db.NguoiDungs
                .Include(x => x.MaVaiTroNavigation)
                .CountAsync(x => x.TrangThai && x.MaId != id && x.MaVaiTroNavigation != null && x.MaVaiTroNavigation.Ten.ToLower().Contains("admin"));
            if (otherAdmins == 0)
                return BadRequest(ApiResponse<object>.Fail("Không thể hạ quyền Quản trị viên cuối cùng của hệ thống."));
        }

        var oldRoleName = u.MaVaiTroNavigation?.Ten ?? "Chưa gán";
        u.MaVaiTro = vaiTro.MaId;
        var isCustomerRole = vaiTro.Ten.ToLower().Contains("customer") || vaiTro.Ten.ToLower().Contains("khach");
        u.LoaiNguoiDung = StatusMapper.NormalizeLoaiNguoiDung(req.LoaiNguoiDung, isCustomerRole ? 6 : 1);

        // Thu hồi các RefreshToken cũ để lần làm mới tiếp theo cấp lại claims đúng vai trò mới
        var activeTokens = await db.RefreshTokens
            .Where(t => t.MaNguoiDung == u.MaId && !t.DaThuHoi)
            .ToListAsync();
        foreach (var t in activeTokens)
        {
            t.DaThuHoi = true;
            t.NgayThuHoi = DateTime.UtcNow;
            t.LyDoThuHoi = $"Đổi vai trò sang {vaiTro.Ten}";
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_VAI_TRO_NGUOI_DUNG",
            doiTuong: "NguoiDung",
            khoaChinh: u.MaId.ToString(),
            noiDung: $"Đổi vai trò của '{u.HoTen}' từ '{oldRoleName}' sang '{vaiTro.Ten}' ({u.LoaiNguoiDung})",
            maNguoiDung: currentUserId,
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, $"Đã cập nhật vai trò thành '{vaiTro.Ten}'."));
    }

    [HttpGet("thong-ke")]
    public async Task<ActionResult<ApiResponse<object>>> ThongKe()
    {
        if (!User.HasPermission(Quyen.ManageUsers) && !User.IsManagerOrAdmin())
            return Forbid();

        var stats = new
        {
            TongNguoiDung = await db.NguoiDungs.CountAsync(),
            DangHoatDong = await db.NguoiDungs.CountAsync(u => u.TrangThai),
            KhachHang = await db.NguoiDungs.CountAsync(u => u.LoaiNguoiDung == "customer" || u.LoaiNguoiDung == "khach_hang"),
            NhanVien = await db.NguoiDungs.CountAsync(u => u.LoaiNguoiDung != "customer" && u.LoaiNguoiDung != "khach_hang"),
            MoiTrongThang = await db.NguoiDungs.CountAsync(u =>
                u.NgayTao >= DateTime.UtcNow.AddMonths(-1))
        };
        return Ok(ApiResponse<object>.Ok(stats));
    }
}
