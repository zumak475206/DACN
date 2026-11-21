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
        if (!User.HasPermission(Quyen.ViewDashboard))
            return Forbid();

        var dauThang = GioVN.DauThang;
        var homNay = GioVN.HomNay;

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
                MaId = d.MaId,
                MaDatPhong = d.MaDatPhong,
                TenKhach = d.TenKhach,
                TongTien = d.TongTien,
                TrangThai = d.TrangThai,
                NgayTao = d.NgayTao
            })
            .ToListAsync();

        return Ok(ApiResponse<DashboardAdminDto>.Ok(new DashboardAdminDto
        {
            TongPhong = tongPhong,
            PhongDangCoKhach = phongCoKhach,
            PhongTrong = phongTrong,
            PhongBaoDuong = phongBaoDuong,
            DoanhThuThang = doanhThuThang,
            DoanhThuHomNay = doanhThuHomNay,
            TongDatPhongHomNay = datPhongHomNay,
            DatPhongChoXacNhan = choXacNhan,
            DatPhongMoiNhat = datPhongMoiNhat
        }));
    }

    [HttpGet("kpi")]
    public async Task<ActionResult<ApiResponse<HotelBiKpiDto>>> Kpi([FromQuery] DateTime? tuNgay = null, [FromQuery] DateTime? denNgay = null)
    {
        if (!User.HasPermission(Quyen.ViewDashboard))
            return Forbid();

        var start = tuNgay ?? GioVN.HomNay.AddDays(-30);
        var end = denNgay ?? GioVN.Now;
        var daysInPeriod = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));

        var tongPhong = Math.Max(1, await db.Phongs.CountAsync());
        var phongCoKhach = await db.Phongs.CountAsync(p => p.TrangThai == "Occupied" || p.TrangThai == "co_khach" || p.TrangThai == "Reserved");

        var bookingsInPeriod = await db.DatPhongs
            .AsNoTracking()
            .Include(d => d.ChiTietDatPhongs)
                .ThenInclude(ct => ct.MaLoaiPhongNavigation)
            .Where(d => d.NgayTao >= start && d.NgayTao <= end)
            .ToListAsync();

        var totalBookings = bookingsInPeriod.Count;
        var cancelledBookings = bookingsInPeriod.Count(b => b.TrangThai == "Cancelled" || b.TrangThai == "da_huy");
        var cancelRate = totalBookings > 0
            ? Math.Round((decimal)cancelledBookings / totalBookings * 100m, 1)
            : 0m;

        var activeBookings = bookingsInPeriod
            .Where(b => b.TrangThai != "Cancelled" && b.TrangThai != "da_huy")
            .ToList();

        var allDetails = activeBookings.SelectMany(b => b.ChiTietDatPhongs).ToList();
        var totalRoomNightsSold = allDetails.Sum(ct => Math.Max(1, (int)Math.Ceiling((ct.NgayTraPhongDuKien - ct.NgayNhanPhongDuKien).TotalDays)));
        var totalRoomRevenue = activeBookings.Sum(b => b.TongTien);

        var occupancyPct = Math.Round((decimal)phongCoKhach / tongPhong * 100m, 1);
        var adr = totalRoomNightsSold > 0
            ? Math.Round(totalRoomRevenue / totalRoomNightsSold, 0)
            : 850_000m;
        var availableRoomNights = tongPhong * daysInPeriod;
        var revPar = availableRoomNights > 0
            ? Math.Round(totalRoomRevenue / availableRoomNights, 0)
            : Math.Round(adr * occupancyPct / 100m, 0);

        var startOnly = DateOnly.FromDateTime(start);
        var endOnly = DateOnly.FromDateTime(end);
        var chiPhiKy = await db.ChiPhiVanHanhs
            .Where(c => c.NgayChi >= startOnly && c.NgayChi <= endOnly)
            .SumAsync(c => (decimal?)c.SoTien) ?? 0m;

        var avgRating = await db.DanhGia
            .Where(d => d.DaDuyet)
            .AverageAsync(d => (double?)d.DiemDanhGia) ?? 4.8;

        var byChannel = activeBookings
            .GroupBy(b => string.IsNullOrWhiteSpace(b.KenhDatPhong) ? "web" : b.KenhDatPhong)
            .Select(g => new DoanhThuTheoKenhDto
            {
                Kenh = g.Key,
                SoDon = g.Count(),
                DoanhThu = g.Sum(x => x.TongTien)
            })
            .OrderByDescending(x => x.DoanhThu)
            .ToList();

        var byRoomType = allDetails
            .GroupBy(ct => ct.MaLoaiPhongNavigation?.Ten ?? $"Hạng phòng #{ct.MaLoaiPhong}")
            .Select(g => new DoanhThuTheoLoaiPhongDto
            {
                TenLoaiPhong = g.Key,
                SoDemBan = g.Sum(ct => Math.Max(1, (int)Math.Ceiling((ct.NgayTraPhongDuKien - ct.NgayNhanPhongDuKien).TotalDays))),
                DoanhThu = g.Sum(ct => ct.GiaMoiDem * Math.Max(1, (int)Math.Ceiling((ct.NgayTraPhongDuKien - ct.NgayNhanPhongDuKien).TotalDays)))
            })
            .OrderByDescending(x => x.DoanhThu)
            .ToList();

        return Ok(ApiResponse<HotelBiKpiDto>.Ok(new HotelBiKpiDto
        {
            CongSuatPhongPhanTram = occupancyPct,
            AdrGiaTrungBinhDem = adr,
            RevParDoanhThuTrenPhong = revPar,
            TyLeHuyPhongPhanTram = cancelRate,
            TongDoanhThuKy = totalRoomRevenue,
            TongChiPhiVanHanhKy = chiPhiKy,
            LoiNhuanRongKy = totalRoomRevenue - chiPhiKy,
            DiemDanhGiaTrungBinh = Math.Round((decimal)avgRating, 2),
            TheoKenhDat = byChannel,
            TheoLoaiPhong = byRoomType
        }));
    }

    [HttpPost("doanh-thu")]
    public async Task<ActionResult<ApiResponse<List<ThongKeDoanhThuDto>>>> DoanhThu([FromBody] ThongKeDoanhThuRequest req)
    {
        if (!User.HasPermission(Quyen.ViewDashboard))
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
