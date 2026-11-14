using DACN.Web.Common;
using DACN.Web.DTOs.Audit;
using DACN.Web.DTOs.Common;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DACN.Web.Controllers;

[Route("api/v1/nhat-ky-he-thong")]
[ApiController]
[Authorize]
public class NhatKyController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<KetQuaPhanTrang<NhatKyHeThongDto>>>> DanhSach([FromQuery] NhatKyFilterRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var (danhSach, tongSo) = await auditService.LayDanhSachAsync(req);

        var result = new KetQuaPhanTrang<NhatKyHeThongDto>
        {
            DuLieu = danhSach,
            TongSo = tongSo,
            Trang = req.Trang,
            SoLuong = req.SoLuong
        };

        return Ok(ApiResponse<KetQuaPhanTrang<NhatKyHeThongDto>>.Ok(result));
    }
}
