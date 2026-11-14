using DACN.Infrastructure.Data;
using DACN.Web.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DACN.Web.Controllers;

[Route("api/v1/system")]
[ApiController]
public class SystemController(DACNContext db, IHostEnvironment env) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> HealthCheck()
    {
        var dbConnected = false;
        string? dbError = null;

        try
        {
            dbConnected = await db.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            dbError = env.IsDevelopment() ? ex.Message : "Lỗi kết nối cơ sở dữ liệu.";
        }

        var isHealthy = dbConnected;

        var healthData = new
        {
            TrangThai = isHealthy ? "Healthy" : "Degraded",
            MoiTruong = env.EnvironmentName,
            ThoiGianMayChu = DateTime.UtcNow,
            CoSoDuLieu = new
            {
                KetNoi = dbConnected,
                Loi = dbError
            },
            PhienBanApi = "1.0.0"
        };

        if (!isHealthy)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail($"Hệ thống cơ sở dữ liệu đang gián đoạn: {dbError}"));
        }

        return Ok(ApiResponse<object>.Ok(healthData, "Hệ thống hoạt động bình thường."));
    }
}
