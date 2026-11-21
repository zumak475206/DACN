using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

public class TaoChiPhiRequest
{
    [Required]
    [MaxLength(100)]
    public string DanhMuc { get; set; } = "Vận hành";
    [Required]
    [MaxLength(500)]
    public string MoTa { get; set; } = string.Empty;
    [Range(1, 10000000000)]
    public decimal SoTien { get; set; }
    public DateTime? NgayChi { get; set; }
}

[Authorize]
[Route("api/v1/chi-phi")]
[ApiController]
public class ChiPhiController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> DanhSach([FromQuery] string? trangThai = null)
    {
        if (!User.HasPermission(Quyen.ManageExpenses))
            return Forbid();

        var query = db.ChiPhiVanHanhs
            .AsNoTracking()
            .Include(c => c.NguoiGhiNhanNavigation)
            .Include(c => c.NguoiPheDuyetNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
        {
            var norm = StatusMapper.NormalizeChiPhiTrangThai(trangThai);
            query = query.Where(c => c.TrangThai == trangThai || c.TrangThai == norm);
        }

        var list = await query
            .OrderByDescending(c => c.NgayChi)
            .Take(100)
            .Select(c => new
            {
                c.MaId,
                c.DanhMuc,
                c.MoTa,
                c.SoTien,
                c.NgayChi,
                c.TrangThai,
                TenNguoiGhiNhan = c.NguoiGhiNhanNavigation.HoTen,
                TenNguoiPheDuyet = c.NguoiPheDuyetNavigation != null ? c.NguoiPheDuyetNavigation.HoTen : null,
                c.NgayTao
            })
            .ToListAsync();

        var tongDaChi = list.Where(x => x.TrangThai == "Paid" || x.TrangThai == "Approved").Sum(x => x.SoTien);
        return Ok(ApiResponse<object>.Ok(new { TongChiPhiDaDuyet = tongDaChi, DanhSach = list }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Tao([FromBody] TaoChiPhiRequest req)
    {
        if (!User.HasPermission(Quyen.ManageExpenses))
            return Forbid();

        var userId = User.GetUserId() ?? 1;
        var cp = new ChiPhiVanHanh
        {
            DanhMuc = req.DanhMuc,
            MoTa = req.MoTa,
            SoTien = req.SoTien,
            NgayChi = DateOnly.FromDateTime(req.NgayChi ?? DateTime.UtcNow),
            NguoiGhiNhan = userId,
            TrangThai = "Pending",
            NgayTao = DateTime.UtcNow
        };
        db.ChiPhiVanHanhs.Add(cp);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "TAO_CHI_PHI_VAN_HANH",
            doiTuong: "ChiPhiVanHanh",
            khoaChinh: cp.MaId.ToString(),
            noiDung: $"Tạo khoản chi phí '{cp.MoTa}' ({cp.DanhMuc}): {cp.SoTien:N0} VNĐ",
            maNguoiDung: userId,
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(new { cp.MaId, cp.TrangThai }, "Đã ghi nhận chi phí vận hành mới."));
    }

    [HttpPut("{id:int}/duyet")]
    public async Task<ActionResult<ApiResponse<object>>> DuyetChiPhi(int id, [FromQuery] string trangThai = "Approved")
    {
        if (!User.IsManagerOrAdmin())
            return Forbid();

        var cp = await db.ChiPhiVanHanhs.FindAsync(id);
        if (cp == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy khoản chi phí."));

        var currentUserId = User.GetUserId();
        if (cp.NguoiGhiNhan == currentUserId)
            return BadRequest(ApiResponse<object>.Fail("Nguyên tắc kiểm soát (SoD): Người lập phiếu chi không được phép tự phê duyệt khoản chi của chính mình."));

        cp.TrangThai = StatusMapper.NormalizeChiPhiTrangThai(trangThai);
        cp.NguoiPheDuyet = currentUserId;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "DUYET_CHI_PHI_VAN_HANH",
            doiTuong: "ChiPhiVanHanh",
            khoaChinh: cp.MaId.ToString(),
            noiDung: $"Phê duyệt khoản chi phí #{cp.MaId} ('{cp.MoTa}', {cp.SoTien:N0} VNĐ) sang trạng thái '{cp.TrangThai}'",
            maNguoiDung: currentUserId,
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(new { cp.MaId, cp.TrangThai }, $"Đã cập nhật trạng thái chi phí sang '{cp.TrangThai}'."));
    }
}
