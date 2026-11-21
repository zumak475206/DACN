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

[Authorize]
[Route("api/v1/loai-phong")]
[ApiController]
public class LoaiPhongController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LoaiPhongDto>>>> DanhSach()
    {
        var data = await db.LoaiPhongs
            .AsNoTracking()
            .Include(lp => lp.AnhPhongs)
            .Include(lp => lp.MaTienNghis)
            .Where(lp => lp.DangHoatDong)
            .Select(lp => new LoaiPhongDto
            {
                MaId = lp.MaId,
                Ten = lp.Ten,
                DuongDanRutGon = lp.DuongDanRutGon,
                GiaCoBan = lp.GiaCoBan,
                SoNguoiLonToiDa = lp.SoNguoiLonToiDa,
                SoTreEmToiDa = lp.SoTreEmToiDa,
                DienTichM2 = lp.DienTichM2,
                LoaiGiuong = lp.LoaiGiuong,
                HuongNhin = lp.HuongNhin,
                MoTa = lp.MoTa,
                DangHoatDong = lp.DangHoatDong,
                SoPhongTrong = lp.Phongs.Count(p => p.TrangThai == "Available" || p.TrangThai == "trong"),
                HinhAnh = lp.AnhPhongs.Select(a => a.DuongDanAnh).ToList(),
                TienNghi = lp.MaTienNghis.Select(t => t.Ten).ToList()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<LoaiPhongDto>>.Ok(data));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<LoaiPhongDto>>> ChiTiet(int id)
    {
        var lp = await db.LoaiPhongs
            .AsNoTracking()
            .Include(x => x.AnhPhongs)
            .Include(x => x.MaTienNghis)
            .Include(x => x.Phongs)
            .FirstOrDefaultAsync(x => x.MaId == id);

        if (lp == null) return NotFound(ApiResponse<LoaiPhongDto>.Fail("Không tìm thấy loại phòng."));

        return Ok(ApiResponse<LoaiPhongDto>.Ok(new LoaiPhongDto
        {
            MaId = lp.MaId, Ten = lp.Ten, DuongDanRutGon = lp.DuongDanRutGon,
            GiaCoBan = lp.GiaCoBan, SoNguoiLonToiDa = lp.SoNguoiLonToiDa,
            SoTreEmToiDa = lp.SoTreEmToiDa, DienTichM2 = lp.DienTichM2,
            LoaiGiuong = lp.LoaiGiuong, HuongNhin = lp.HuongNhin, MoTa = lp.MoTa,
            DangHoatDong = lp.DangHoatDong,
            SoPhongTrong = lp.Phongs.Count(p => p.TrangThai == "Available" || p.TrangThai == "trong"),
            HinhAnh = lp.AnhPhongs.Select(a => a.DuongDanAnh).ToList(),
            TienNghi = lp.MaTienNghis.Select(t => t.Ten).ToList()
        }));
    }

    [AllowAnonymous]
    [HttpGet("tim-kiem")]
    public async Task<ActionResult<ApiResponse<List<LoaiPhongDto>>>> TimKiem([FromQuery] TimKiemPhongRequest req)
    {
        if (req.NgayNhanPhong.Date < DateTime.UtcNow.Date)
            return BadRequest(ApiResponse<List<LoaiPhongDto>>.Fail("Ngày nhận phòng không thể trong quá khứ."));

        if (req.NgayNhanPhong.Date >= req.NgayTraPhong.Date)
            return BadRequest(ApiResponse<List<LoaiPhongDto>>.Fail("Ngày nhận phòng phải trước ngày trả phòng ít nhất 1 ngày."));

        var loaiPhongConflict = await db.ChiTietDatPhongs
            .Where(ct => ct.TrangThai != "Cancelled" && ct.TrangThai != "da_huy"
                && ct.NgayNhanPhongDuKien < req.NgayTraPhong
                && ct.NgayTraPhongDuKien > req.NgayNhanPhong)
            .GroupBy(ct => ct.MaLoaiPhong)
            .Select(g => new { MaLoaiPhong = g.Key, SoDangDung = g.Count() })
            .ToListAsync();

        var data = await db.LoaiPhongs
            .AsNoTracking()
            .Include(lp => lp.AnhPhongs)
            .Include(lp => lp.MaTienNghis)
            .Include(lp => lp.Phongs)
            .Where(lp => lp.DangHoatDong
                && lp.SoNguoiLonToiDa >= req.SoNguoiLon
                && lp.SoTreEmToiDa >= req.SoTreEm)
            .ToListAsync();

        var result = data.Select(lp =>
        {
            var totalPhong = lp.Phongs.Count(p => p.TrangThai != "Maintenance" && p.TrangThai != "bao_duong");
            var dangDung = loaiPhongConflict.FirstOrDefault(x => x.MaLoaiPhong == lp.MaId)?.SoDangDung ?? 0;
            return new LoaiPhongDto
            {
                MaId = lp.MaId, Ten = lp.Ten, DuongDanRutGon = lp.DuongDanRutGon,
                GiaCoBan = lp.GiaCoBan, SoNguoiLonToiDa = lp.SoNguoiLonToiDa,
                SoTreEmToiDa = lp.SoTreEmToiDa, DienTichM2 = lp.DienTichM2,
                LoaiGiuong = lp.LoaiGiuong, HuongNhin = lp.HuongNhin, MoTa = lp.MoTa,
                DangHoatDong = lp.DangHoatDong,
                SoPhongTrong = Math.Max(0, totalPhong - dangDung),
                HinhAnh = lp.AnhPhongs.Select(a => a.DuongDanAnh).ToList(),
                TienNghi = lp.MaTienNghis.Select(t => t.Ten).ToList()
            };
        }).Where(lp => lp.SoPhongTrong > 0).ToList();

        return Ok(ApiResponse<List<LoaiPhongDto>>.Ok(result));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LoaiPhongDto>>> Tao([FromBody] TaoLoaiPhongRequest req)
    {
        if (!User.HasPermission(Quyen.ManageRooms))
            return Forbid();

        if (await db.LoaiPhongs.AnyAsync(lp => lp.DuongDanRutGon == req.DuongDanRutGon))
            return BadRequest(ApiResponse<LoaiPhongDto>.Fail("Đường dẫn rút gọn đã tồn tại."));

        var lp = new LoaiPhong
        {
            Ten = req.Ten, DuongDanRutGon = req.DuongDanRutGon,
            GiaCoBan = req.GiaCoBan, SoNguoiLonToiDa = req.SoNguoiLonToiDa,
            SoTreEmToiDa = req.SoTreEmToiDa, DienTichM2 = (int?)req.DienTichM2,
            LoaiGiuong = req.LoaiGiuong, HuongNhin = req.HuongNhin,
            MoTa = req.MoTa, NoiDung = req.NoiDung,
            DangHoatDong = true, NgayTao = DateTime.UtcNow
        };
        db.LoaiPhongs.Add(lp);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "TAO_LOAI_PHONG",
            doiTuong: "LoaiPhong",
            khoaChinh: lp.MaId.ToString(),
            noiDung: $"Tạo loại phòng mới '{lp.Ten}' (Giá cơ bản: {lp.GiaCoBan:N0} VNĐ)",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<LoaiPhongDto>.Ok(new LoaiPhongDto { MaId = lp.MaId, Ten = lp.Ten, GiaCoBan = lp.GiaCoBan, DangHoatDong = true }, "Tạo loại phòng thành công."));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhat(int id, [FromBody] TaoLoaiPhongRequest req)
    {
        if (!User.HasPermission(Quyen.ManageRooms))
            return Forbid();

        var lp = await db.LoaiPhongs.FindAsync(id);
        if (lp == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy loại phòng."));

        lp.Ten = req.Ten; lp.GiaCoBan = req.GiaCoBan;
        lp.SoNguoiLonToiDa = req.SoNguoiLonToiDa; lp.SoTreEmToiDa = req.SoTreEmToiDa;
        lp.DienTichM2 = (int?)req.DienTichM2; lp.LoaiGiuong = req.LoaiGiuong;
        lp.HuongNhin = req.HuongNhin; lp.MoTa = req.MoTa; lp.NoiDung = req.NoiDung;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_LOAI_PHONG",
            doiTuong: "LoaiPhong",
            khoaChinh: lp.MaId.ToString(),
            noiDung: $"Cập nhật loại phòng '{lp.Ten}' (Giá cơ bản: {lp.GiaCoBan:N0} VNĐ)",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật thành công."));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Xoa(int id)
    {
        if (!User.HasPermission(Quyen.ManageRooms))
            return Forbid();

        var lp = await db.LoaiPhongs.FindAsync(id);
        if (lp == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy loại phòng."));
        lp.DangHoatDong = false;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "XOA_LOAI_PHONG",
            doiTuong: "LoaiPhong",
            khoaChinh: lp.MaId.ToString(),
            noiDung: $"Vô hiệu hóa loại phòng '{lp.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đã vô hiệu hóa loại phòng."));
    }
}
