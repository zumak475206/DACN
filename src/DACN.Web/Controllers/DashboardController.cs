using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Authorize]
[Route("api/v1/dashboard")]
[ApiController]
public class DashboardController(DACNContext db) : ControllerBase
{
    [HttpGet("tong-quan")]
    public async Task<ActionResult<ApiResponse<DashboardAdminDto>>> TongQuan()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var now = DateTime.UtcNow;
        var dauThang = new DateTime(now.Year, now.Month, 1);
        var homNay = now.Date;

        var tongPhong = await db.Phongs.CountAsync();
        var phongCoKhach = await db.Phongs.CountAsync(p => p.TrangThai == "Occupied" || p.TrangThai == "co_khach");
        var phongTrong = await db.Phongs.CountAsync(p => p.TrangThai == "Available" || p.TrangThai == "trong");
        var phongBaoDuong = await db.Phongs.CountAsync(p => p.TrangThai == "Maintenance" || p.TrangThai == "bao_duong");

        var doanhThuThang = await db.HoaDons
            .Where(h => h.NgayTao >= dauThang && (h.TrangThai == "Paid" || h.TrangThai == "da_thanh_toan"))
            .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;

        var doanhThuHomNay = await db.HoaDons
            .Where(h => h.NgayTao >= homNay && (h.TrangThai == "Paid" || h.TrangThai == "da_thanh_toan"))
            .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;

        var datPhongHomNay = await db.DatPhongs.CountAsync(d => d.NgayTao >= homNay);
        var choXacNhan = await db.DatPhongs.CountAsync(d => d.TrangThai == "Pending" || d.TrangThai == "cho_xac_nhan");

        var datPhongMoiNhat = await db.DatPhongs
            .AsNoTracking()
            .OrderByDescending(d => d.NgayTao)
            .Take(10)
            .Select(d => new DatPhongMoiDto
            {
                MaId = d.MaId, MaDatPhong = d.MaDatPhong,
                TenKhach = d.TenKhach, TongTien = d.TongTien,
                TrangThai = d.TrangThai, NgayTao = d.NgayTao
            })
            .ToListAsync();

        return Ok(ApiResponse<DashboardAdminDto>.Ok(new DashboardAdminDto
        {
            TongPhong = tongPhong, PhongDangCoKhach = phongCoKhach,
            PhongTrong = phongTrong, PhongBaoDuong = phongBaoDuong,
            DoanhThuThang = doanhThuThang, DoanhThuHomNay = doanhThuHomNay,
            TongDatPhongHomNay = datPhongHomNay, DatPhongChoXacNhan = choXacNhan,
            DatPhongMoiNhat = datPhongMoiNhat
        }));
    }

    [HttpPost("doanh-thu")]
    public async Task<ActionResult<ApiResponse<List<ThongKeDoanhThuDto>>>> DoanhThu([FromBody] ThongKeDoanhThuRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        if (req.TuNgay >= req.DenNgay)
            return BadRequest(ApiResponse<List<ThongKeDoanhThuDto>>.Fail("Từ ngày phải trước đến ngày."));

        var data = await db.HoaDons
            .AsNoTracking()
            .Where(h => h.NgayTao >= req.TuNgay && h.NgayTao <= req.DenNgay && (h.TrangThai == "Paid" || h.TrangThai == "da_thanh_toan"))
            .GroupBy(h => h.NgayTao.Date)
            .Select(g => new ThongKeDoanhThuDto
            {
                Ngay = g.Key,
                DoanhThu = g.Sum(h => h.TongThanhToan),
                SoDatPhong = g.Count()
            })
            .OrderBy(x => x.Ngay)
            .ToListAsync();

        return Ok(ApiResponse<List<ThongKeDoanhThuDto>>.Ok(data));
    }

    [HttpGet("phong-status")]
    public async Task<ActionResult<ApiResponse<object>>> PhongStatus()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var data = await db.Phongs
            .AsNoTracking()
            .GroupBy(p => p.TrangThai)
            .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(data));
    }
}
