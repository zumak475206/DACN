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

public class TaoCapNhatVaiTroRequest
{
    [Required(ErrorMessage = "Tên vai trò không được để trống.")]
    [MaxLength(100)]
    public string Ten { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MoTa { get; set; }
}

[Authorize]
[Route("api/v1/vai-tro")]
[ApiController]
public class VaiTroController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachVaiTro()
    {
        if (!User.HasPermission(Quyen.ManageUsers) && !User.IsManagerOrAdmin())
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
        if (!User.HasPermission(Quyen.ManageUsers) && !User.IsManagerOrAdmin())
            return Forbid();

        var perms = await db.QuyenHans
            .AsNoTracking()
            .OrderBy(q => q.PhanHe)
            .ThenBy(q => q.MaId)
            .Select(q => new { q.MaId, q.Ten, q.PhanHe, q.MoTa })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(perms));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> TaoVaiTro([FromBody] TaoCapNhatVaiTroRequest req)
    {
        if (!User.HasPermission(Quyen.ManageUsers))
            return Forbid();

        var tenTrim = req.Ten.Trim();
        if (await db.VaiTros.AnyAsync(v => v.Ten.ToLower() == tenTrim.ToLower()))
            return BadRequest(ApiResponse<object>.Fail("Tên vai trò đã tồn tại."));

        var vaiTro = new VaiTro
        {
            Ten = tenTrim,
            MoTa = req.MoTa?.Trim()
        };
        db.VaiTros.Add(vaiTro);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "TAO_VAI_TRO",
            doiTuong: "VaiTro",
            khoaChinh: vaiTro.MaId.ToString(),
            noiDung: $"Tạo vai trò mới '{vaiTro.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(new { vaiTro.MaId, vaiTro.Ten, vaiTro.MoTa }, "Tạo vai trò thành công."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatVaiTro(int id, [FromBody] TaoCapNhatVaiTroRequest req)
    {
        if (!User.HasPermission(Quyen.ManageUsers))
            return Forbid();

        var vaiTro = await db.VaiTros.FindAsync(id);
        if (vaiTro == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy vai trò."));

        var isSystemAdmin = vaiTro.Ten.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        if (isSystemAdmin && !req.Ten.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Không được phép đổi tên vai trò hệ thống 'Admin'."));

        var tenTrim = req.Ten.Trim();
        if (await db.VaiTros.AnyAsync(v => v.MaId != id && v.Ten.ToLower() == tenTrim.ToLower()))
            return BadRequest(ApiResponse<object>.Fail("Tên vai trò đã tồn tại."));

        vaiTro.Ten = tenTrim;
        vaiTro.MoTa = req.MoTa?.Trim();
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_VAI_TRO",
            doiTuong: "VaiTro",
            khoaChinh: vaiTro.MaId.ToString(),
            noiDung: $"Cập nhật thông tin vai trò '{vaiTro.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật vai trò thành công."));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> XoaVaiTro(int id)
    {
        if (!User.HasPermission(Quyen.ManageUsers))
            return Forbid();

        var vaiTro = await db.VaiTros
            .Include(v => v.NguoiDungs)
            .Include(v => v.MaQuyenHans)
            .FirstOrDefaultAsync(v => v.MaId == id);
        if (vaiTro == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy vai trò."));

        if (vaiTro.Ten.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            vaiTro.Ten.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Không thể xóa vai trò gốc của hệ thống (Admin / Customer)."));

        if (vaiTro.NguoiDungs.Any())
            return BadRequest(ApiResponse<object>.Fail($"Không thể xóa vai trò '{vaiTro.Ten}' vì đang có {vaiTro.NguoiDungs.Count} người dùng thuộc vai trò này."));

        vaiTro.MaQuyenHans.Clear();
        db.VaiTros.Remove(vaiTro);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "XOA_VAI_TRO",
            doiTuong: "VaiTro",
            khoaChinh: id.ToString(),
            noiDung: $"Xóa vai trò '{vaiTro.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, $"Đã xóa vai trò '{vaiTro.Ten}'."));
    }

    [HttpPut("{id:int}/quyen-han")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatQuyenVaiTro(int id, [FromBody] List<int>? danhSachMaQuyen)
    {
        if (!User.HasPermission(Quyen.ManageUsers))
            return Forbid();

        danhSachMaQuyen ??= new List<int>();
        var distinctIds = danhSachMaQuyen.Distinct().ToList();

        var vaiTro = await db.VaiTros.Include(v => v.MaQuyenHans).FirstOrDefaultAsync(v => v.MaId == id);
        if (vaiTro == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy vai trò."));

        var quyens = await db.QuyenHans.Where(q => distinctIds.Contains(q.MaId)).ToListAsync();
        if (quyens.Count != distinctIds.Count)
            return BadRequest(ApiResponse<object>.Fail("Một hoặc nhiều mã quyền hạn không tồn tại trong hệ thống."));

        var isSystemAdmin = vaiTro.Ten.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        if (isSystemAdmin && !quyens.Any(q => q.Ten.Equals(Quyen.ManageUsers, StringComparison.OrdinalIgnoreCase)))
            return BadRequest(ApiResponse<object>.Fail("Không thể gỡ quyền 'ManageUsers' khỏi vai trò Admin."));

        var oldPermNames = vaiTro.MaQuyenHans.Select(q => q.Ten).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newPermNames = quyens.Select(q => q.Ten).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = newPermNames.Except(oldPermNames).ToList();
        var removed = oldPermNames.Except(newPermNames).ToList();

        vaiTro.MaQuyenHans.Clear();
        foreach (var q in quyens)
        {
            vaiTro.MaQuyenHans.Add(q);
        }

        await db.SaveChangesAsync();

        var diffText = $"Tổng {quyens.Count} quyền"
            + (added.Count > 0 ? $" | +Thêm: [{string.Join(", ", added)}]" : "")
            + (removed.Count > 0 ? $" | -Bớt: [{string.Join(", ", removed)}]" : "");

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_QUYEN_VAI_TRO",
            doiTuong: "VaiTro",
            khoaChinh: vaiTro.MaId.ToString(),
            noiDung: $"Cập nhật quyền cho vai trò '{vaiTro.Ten}': {diffText}",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, $"Đã cập nhật {quyens.Count} quyền cho vai trò '{vaiTro.Ten}'."));
    }
}
