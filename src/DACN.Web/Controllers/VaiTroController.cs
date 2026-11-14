using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Authorize]
[Route("api/v1/vai-tro")]
[ApiController]
public class VaiTroController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachVaiTro()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var roles = await db.VaiTros
            .AsNoTracking()
            .Include(v => v.MaQuyenHans)
            .Select(v => new
            {
                v.MaId,
                v.Ten,
                v.MoTa,
                SoNguoiDung = v.NguoiDungs.Count,
                DanhSachQuyen = v.MaQuyenHans.Select(q => new { q.MaId, q.Ten, q.PhanHe, q.MoTa }).ToList()
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(roles));
    }

    [HttpGet("quyen-han")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachQuyenHan()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var perms = await db.QuyenHans
            .AsNoTracking()
            .OrderBy(q => q.PhanHe)
            .ThenBy(q => q.MaId)
            .Select(q => new { q.MaId, q.Ten, q.PhanHe, q.MoTa })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(perms));
    }

    [HttpPut("{id:int}/quyen-han")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatQuyenVaiTro(int id, [FromBody] List<int> danhSachMaQuyen)
    {
        if (!User.IsAdmin() && !User.HasPermission("ManageUsers"))
            return Forbid();

        var vaiTro = await db.VaiTros.Include(v => v.MaQuyenHans).FirstOrDefaultAsync(v => v.MaId == id);
        if (vaiTro == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy vai trò."));

        var quyens = await db.QuyenHans.Where(q => danhSachMaQuyen.Contains(q.MaId)).ToListAsync();
        vaiTro.MaQuyenHans.Clear();
        foreach (var q in quyens)
        {
            vaiTro.MaQuyenHans.Add(q);
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_QUYEN_VAI_TRO",
            doiTuong: "VaiTro",
            khoaChinh: vaiTro.MaId.ToString(),
            noiDung: $"Cập nhật {quyens.Count} quyền hạn cho vai trò '{vaiTro.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, $"Đã cập nhật {quyens.Count} quyền cho vai trò '{vaiTro.Ten}'."));
    }
}
