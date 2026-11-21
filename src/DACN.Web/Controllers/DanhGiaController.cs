using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.DanhGia;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Authorize]
[Route("api/v1/danh-gia")]
[ApiController]
public class DanhGiaController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("loai-phong/{maLoaiPhong:int}")]
    public async Task<ActionResult<ApiResponse<List<DanhGiaDto>>>> TheoLoaiPhong(int maLoaiPhong)
    {
        var data = await db.DanhGia
            .AsNoTracking()
            .Include(d => d.MaNguoiDungNavigation)
            .Include(d => d.MaLoaiPhongNavigation)
            .Where(d => d.MaLoaiPhong == maLoaiPhong && d.DaDuyet)
            .OrderByDescending(d => d.NgayTao)
            .Select(d => new DanhGiaDto
            {
                MaId = d.MaId,
                TenNguoiDung = d.MaNguoiDungNavigation != null ? d.MaNguoiDungNavigation.HoTen : "Khách hàng",
                DiemDanhGia = d.DiemDanhGia,
                BinhLuan = d.BinhLuan,
                DaDuyet = d.DaDuyet,
                NgayTao = d.NgayTao,
                MaLoaiPhong = d.MaLoaiPhong,
                TenLoaiPhong = d.MaLoaiPhongNavigation != null ? d.MaLoaiPhongNavigation.Ten : ""
            })
            .ToListAsync();
        return Ok(ApiResponse<List<DanhGiaDto>>.Ok(data));
    }

    [Authorize]
    [HttpGet("cho-duyet")]
    public async Task<ActionResult<ApiResponse<List<DanhGiaDto>>>> ChoXetDuyet()
    {
        if (!User.HasPermission(Quyen.ModerateReviews))
            return Forbid();

        var data = await db.DanhGia
            .AsNoTracking()
            .Include(d => d.MaNguoiDungNavigation)
            .Include(d => d.MaLoaiPhongNavigation)
            .Where(d => !d.DaDuyet)
            .OrderByDescending(d => d.NgayTao)
            .Select(d => new DanhGiaDto
            {
                MaId = d.MaId,
                TenNguoiDung = d.MaNguoiDungNavigation != null ? d.MaNguoiDungNavigation.HoTen : "Khách hàng",
                DiemDanhGia = d.DiemDanhGia,
                BinhLuan = d.BinhLuan,
                DaDuyet = d.DaDuyet,
                NgayTao = d.NgayTao,
                MaLoaiPhong = d.MaLoaiPhong,
                TenLoaiPhong = d.MaLoaiPhongNavigation != null ? d.MaLoaiPhongNavigation.Ten : ""
            })
            .ToListAsync();
        return Ok(ApiResponse<List<DanhGiaDto>>.Ok(data));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DanhGiaDto>>> Tao([FromBody] TaoDanhGiaRequest req)
    {
        var currentUserId = User.GetUserId();
        if (!currentUserId.HasValue)
            return Unauthorized(ApiResponse<DanhGiaDto>.Fail("Vui lòng đăng nhập để gửi đánh giá."));

        if (req.DiemDanhGia < 1 || req.DiemDanhGia > 5)
            return BadRequest(ApiResponse<DanhGiaDto>.Fail("Điểm đánh giá phải từ 1 đến 5 sao."));

        var loaiPhong = await db.LoaiPhongs.FindAsync(req.MaLoaiPhong);
        if (loaiPhong == null)
            return NotFound(ApiResponse<DanhGiaDto>.Fail("Không tìm thấy loại phòng."));

        if (req.MaDatPhong.HasValue)
        {
            var datPhong = await db.DatPhongs.FindAsync(req.MaDatPhong.Value);
            if (datPhong == null || (!User.IsStaffOrAdmin() && datPhong.MaNguoiDung != currentUserId.Value))
                return BadRequest(ApiResponse<DanhGiaDto>.Fail("Mã đặt phòng không hợp lệ hoặc không thuộc về bạn."));
        }

        var dg = new DanhGium
        {
            MaNguoiDung = currentUserId.Value,
            MaLoaiPhong = req.MaLoaiPhong,
            MaDatPhong = req.MaDatPhong,
            DiemDanhGia = req.DiemDanhGia,
            BinhLuan = req.BinhLuan,
            DaDuyet = false,
            NgayTao = DateTime.UtcNow
        };
        db.DanhGia.Add(dg);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<DanhGiaDto>.Ok(new DanhGiaDto
        { MaId = dg.MaId, DiemDanhGia = dg.DiemDanhGia, BinhLuan = dg.BinhLuan, DaDuyet = false, NgayTao = dg.NgayTao },
        "Đánh giá đã được gửi và đang chờ kiểm duyệt."));
    }

    [Authorize]
    [HttpPut("{id:int}/duyet")]
    public async Task<ActionResult<ApiResponse<object>>> Duyet(int id)
    {
        if (!User.HasPermission(Quyen.ModerateReviews))
            return Forbid();

        var dg = await db.DanhGia.FindAsync(id);
        if (dg == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đánh giá."));
        dg.DaDuyet = true;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "DUYET_DANH_GIA",
            doiTuong: "DanhGia",
            khoaChinh: id.ToString(),
            noiDung: $"Duyệt đánh giá #{id} ({dg.DiemDanhGia} sao)",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Duyệt đánh giá thành công."));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Xoa(int id)
    {
        if (!User.HasPermission(Quyen.ModerateReviews))
            return Forbid();

        var dg = await db.DanhGia.FindAsync(id);
        if (dg == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đánh giá."));
        db.DanhGia.Remove(dg);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "XOA_DANH_GIA",
            doiTuong: "DanhGia",
            khoaChinh: id.ToString(),
            noiDung: $"Xóa đánh giá #{id}",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đã xóa đánh giá."));
    }
}
