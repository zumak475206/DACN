using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.Phong;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Route("api/v1/phong")]
[ApiController]
public class PhongController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<KetQuaPhanTrang<PhongDto>>>> DanhSach([FromQuery] PhanTrangRequest req)
    {
        var query = db.Phongs.AsNoTracking().Include(p => p.MaLoaiPhongNavigation).AsQueryable();
        var total = await query.CountAsync();
        var data = await query
            .OrderBy(p => p.SoPhong)
            .Skip((req.Trang - 1) * req.SoLuong)
            .Take(req.SoLuong)
            .Select(p => new PhongDto
            {
                MaId = p.MaId, MaLoaiPhong = p.MaLoaiPhong,
                TenLoaiPhong = p.MaLoaiPhongNavigation != null ? p.MaLoaiPhongNavigation.Ten : "",
                SoPhong = p.SoPhong, Tang = p.Tang,
                TrangThai = p.TrangThai, TrangThaiVeSinh = p.TrangThaiVeSinh,
                SoMayLe = p.SoMayLe
            }).ToListAsync();

        return Ok(ApiResponse<KetQuaPhanTrang<PhongDto>>.Ok(new KetQuaPhanTrang<PhongDto>
        { DuLieu = data, TongSo = total, Trang = req.Trang, SoLuong = req.SoLuong }));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PhongDto>>> ChiTiet(int id)
    {
        var p = await db.Phongs.AsNoTracking().Include(x => x.MaLoaiPhongNavigation).FirstOrDefaultAsync(x => x.MaId == id);
        if (p == null) return NotFound(ApiResponse<PhongDto>.Fail("Không tìm thấy phòng."));
        return Ok(ApiResponse<PhongDto>.Ok(new PhongDto
        {
            MaId = p.MaId, MaLoaiPhong = p.MaLoaiPhong,
            TenLoaiPhong = p.MaLoaiPhongNavigation?.Ten ?? "",
            SoPhong = p.SoPhong, Tang = p.Tang,
            TrangThai = p.TrangThai, TrangThaiVeSinh = p.TrangThaiVeSinh,
            SoMayLe = p.SoMayLe
        }));
    }

    [AllowAnonymous]
    [HttpGet("trang-thai")]
    public async Task<ActionResult<ApiResponse<object>>> TheoTrangThai()
    {
        var data = await db.Phongs
            .AsNoTracking()
            .GroupBy(p => p.TrangThai)
            .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(data));
    }

    [Authorize]
    [HttpPut("{id:int}/trang-thai")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatTrangThai(int id, [FromBody] CapNhatTrangThaiPhongRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var p = await db.Phongs.FindAsync(id);
        if (p == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy phòng."));
        var oldStatus = p.TrangThai;
        p.TrangThai = StatusMapper.NormalizePhongTrangThai(req.TrangThai);
        if (req.TrangThaiVeSinh != null)
            p.TrangThaiVeSinh = StatusMapper.NormalizePhongVeSinh(req.TrangThaiVeSinh);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_TRANG_THAI_PHONG",
            doiTuong: "Phong",
            khoaChinh: p.SoPhong,
            noiDung: $"Đổi trạng thái phòng '{p.SoPhong}' từ '{oldStatus}' sang '{p.TrangThai}'. Vệ sinh: {p.TrangThaiVeSinh}",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật trạng thái phòng thành công."));
    }

    [Authorize]
    [HttpPut("{id:int}/hoan-tat-ve-sinh")]
    public async Task<ActionResult<ApiResponse<object>>> HoanTatVeSinh(int id, [FromQuery] string? ghiChu = null)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var p = await db.Phongs.FindAsync(id);
        if (p == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy phòng."));

        if (p.TrangThai != "Occupied" && p.TrangThai != "Reserved")
            p.TrangThai = "Available";
        p.TrangThaiVeSinh = "Clean";

        var nhatKy = await db.NhatKyBuongPhongs
            .Where(n => n.MaPhong == id && (n.TrangThai == "assigned" || n.TrangThai == "in_progress" || n.TrangThai == "cho_xu_ly" || n.TrangThai == "dang_xu_ly"))
            .OrderByDescending(n => n.NgayTao)
            .FirstOrDefaultAsync();

        if (nhatKy != null)
        {
            nhatKy.TrangThai = "done";
            nhatKy.ThoiGianHoanTat = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(ghiChu))
                nhatKy.GhiChuKiemTra = (nhatKy.GhiChuKiemTra + " | " + ghiChu).Trim();
            nhatKy.MaNhanVien = User.GetUserId();
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "HOAN_TAT_VE_SINH_PHONG",
            doiTuong: "Phong",
            khoaChinh: p.SoPhong,
            noiDung: $"Phòng '{p.SoPhong}' đã hoàn tất dọn vệ sinh. Trạng thái phòng chuyển sang 'Available' / 'Clean'.",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, $"Phòng '{p.SoPhong}' đã hoàn tất dọn dẹp và sẵn sàng đón khách."));
    }
}
