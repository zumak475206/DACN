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
    public async Task<ActionResult<ApiResponse<KetQuaPhanTrang<DangNhapResponse>>>> DanhSach([FromQuery] PhanTrangRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var query = db.NguoiDungs.AsNoTracking().Include(u => u.MaVaiTroNavigation).AsQueryable();
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
            TenVaiTro = u.MaVaiTroNavigation?.Ten ?? "Khách hàng", TongDiem = u.TongDiem
        }));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhat(int id, [FromBody] CapNhatHoSoRequest req)
    {
        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && id != currentUserId)
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
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var currentUserId = User.GetUserId();
        if (id == currentUserId && !trangThai)
            return BadRequest(ApiResponse<object>.Fail("Không thể tự vô hiệu hóa tài khoản của chính mình."));

        var u = await db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

        u.TrangThai = trangThai;
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

        return Ok(ApiResponse< object>.Ok(null!, trangThai ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản."));
    }

    [HttpPut("{id:int}/vai-tro")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatVaiTro(int id, [FromBody] CapNhatVaiTroNguoiDungRequest req)
    {
        if (!User.IsAdmin() && !User.HasPermission("ManageUsers"))
            return Forbid();

        var u = await db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

        var vaiTro = await db.VaiTros.FindAsync(req.MaVaiTro);
        if (vaiTro == null) return BadRequest(ApiResponse<object>.Fail("Vai trò không tồn tại."));

        u.MaVaiTro = vaiTro.MaId;
        var isCustomerRole = vaiTro.Ten.ToLower().Contains("customer") || vaiTro.Ten.ToLower().Contains("khach");
        u.LoaiNguoiDung = StatusMapper.NormalizeLoaiNguoiDung(req.LoaiNguoiDung, isCustomerRole ? 6 : 1);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_VAI_TRO_NGUOI_DUNG",
            doiTuong: "NguoiDung",
            khoaChinh: u.MaId.ToString(),
            noiDung: $"Đổi vai trò của '{u.HoTen}' sang '{vaiTro.Ten}' ({u.LoaiNguoiDung})",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, $"Đã cập nhật vai trò thành '{vaiTro.Ten}'."));
    }

    [HttpGet("thong-ke")]
    public async Task<ActionResult<ApiResponse<object>>> ThongKe()
    {
        if (!User.IsStaffOrAdmin())
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
