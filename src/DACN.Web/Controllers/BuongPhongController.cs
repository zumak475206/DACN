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

public class PhanCongDonPhongRequest
{
    [Required]
    public int MaPhong { get; set; }
    public int? MaNhanVien { get; set; }
    public int? MaChiTietDatPhong { get; set; }
    public string? GhiChu { get; set; }
}

public class CapNhatNhiemVuBuongPhongRequest
{
    [Required]
    public string TrangThai { get; set; } = "done";
    public string? GhiChuKiemTra { get; set; }
}

public class GhiNhanHuHongRequest
{
    [Required]
    public int MaChiTietDatPhong { get; set; }
    public int? MaTaiSanPhong { get; set; }
    [Range(1, 100)]
    public int SoLuong { get; set; } = 1;
    [Range(0, 1000000000)]
    public decimal TienPhat { get; set; }
    [Required]
    [MaxLength(500)]
    public string MoTa { get; set; } = string.Empty;
    public string? DuongDanAnh { get; set; }
}

[Authorize]
[Route("api/v1/buong-phong")]
[ApiController]
public class BuongPhongController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [HttpGet("nhiem-vu")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachNhiemVu([FromQuery] string? trangThai = null)
    {
        if (!User.HasPermission(Quyen.ManageHousekeeping) && !User.HasPermission(Quyen.UpdateRoomStatus))
            return Forbid();

        var query = db.NhatKyBuongPhongs
            .AsNoTracking()
            .Include(n => n.MaPhongNavigation).ThenInclude(p => p.MaLoaiPhongNavigation)
            .Include(n => n.MaNhanVienNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
        {
            var norm = StatusMapper.NormalizeBuongPhongTrangThai(trangThai);
            query = query.Where(n => n.TrangThai == trangThai || n.TrangThai == norm);
        }

        var list = await query
            .OrderByDescending(n => n.NgayTao)
            .Take(100)
            .Select(n => new
            {
                n.MaId,
                n.MaPhong,
                SoPhong = n.MaPhongNavigation.SoPhong,
                Tang = n.MaPhongNavigation.Tang,
                TenLoaiPhong = n.MaPhongNavigation.MaLoaiPhongNavigation.Ten,
                n.MaNhanVien,
                TenNhanVien = n.MaNhanVienNavigation != null ? n.MaNhanVienNavigation.HoTen : "Chưa phân công",
                n.MaChiTietDatPhong,
                n.TrangThai,
                n.ThoiGianPhanCong,
                n.ThoiGianBatDau,
                n.ThoiGianHoanTat,
                n.GhiChuKiemTra,
                n.NgayTao
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpPost("phan-cong")]
    public async Task<ActionResult<ApiResponse<object>>> PhanCong([FromBody] PhanCongDonPhongRequest req)
    {
        if (!User.HasPermission(Quyen.ManageHousekeeping))
            return Forbid();

        var phong = await db.Phongs.FindAsync(req.MaPhong);
        if (phong == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy phòng."));

        phong.TrangThaiVeSinh = "Dirty";

        var log = new NhatKyBuongPhong
        {
            MaPhong = req.MaPhong,
            MaNhanVien = req.MaNhanVien ?? User.GetUserId(),
            MaChiTietDatPhong = req.MaChiTietDatPhong,
            TrangThai = "assigned",
            ThoiGianPhanCong = DateTime.UtcNow,
            GhiChuKiemTra = req.GhiChu ?? $"Phân công dọn vệ sinh phòng {phong.SoPhong}",
            NgayTao = DateTime.UtcNow
        };
        db.NhatKyBuongPhongs.Add(log);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "PHAN_CONG_BUONG_PHONG",
            doiTuong: "NhatKyBuongPhong",
            khoaChinh: log.MaId.ToString(),
            noiDung: $"Phân công dọn phòng {phong.SoPhong} (NV #{log.MaNhanVien})",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(new { log.MaId, log.TrangThai }, $"Đã tạo nhiệm vụ dọn phòng {phong.SoPhong}."));
    }

    [HttpPut("nhiem-vu/{id:int}/trang-thai")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatTrangThaiNhiemVu(int id, [FromBody] CapNhatNhiemVuBuongPhongRequest req)
    {
        if (!User.HasPermission(Quyen.ManageHousekeeping) && !User.HasPermission(Quyen.UpdateRoomStatus))
            return Forbid();

        var log = await db.NhatKyBuongPhongs
            .Include(n => n.MaPhongNavigation)
            .FirstOrDefaultAsync(n => n.MaId == id);
        if (log == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy nhiệm vụ buồng phòng."));

        var norm = StatusMapper.NormalizeBuongPhongTrangThai(req.TrangThai);
        log.TrangThai = norm;
        if (!string.IsNullOrWhiteSpace(req.GhiChuKiemTra))
            log.GhiChuKiemTra = req.GhiChuKiemTra;

        if (norm == "in_progress")
        {
            log.ThoiGianBatDau ??= DateTime.UtcNow;
            if (log.MaPhongNavigation != null)
                log.MaPhongNavigation.TrangThaiVeSinh = "Inspecting";
        }
        else if (norm is "done" or "inspected")
        {
            log.ThoiGianHoanTat = DateTime.UtcNow;
            log.MaNhanVien ??= User.GetUserId();
            if (log.MaPhongNavigation != null)
            {
                log.MaPhongNavigation.TrangThaiVeSinh = "Clean";
                if (log.MaPhongNavigation.TrangThai != "Occupied" && log.MaPhongNavigation.TrangThai != "Reserved")
                    log.MaPhongNavigation.TrangThai = "Available";
            }
        }

        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { log.MaId, log.TrangThai }, "Đã cập nhật trạng thái nhiệm vụ buồng phòng."));
    }

    [HttpGet("tai-san")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachTaiSan([FromQuery] int? maPhong = null)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var query = db.TaiSanPhongs
            .AsNoTracking()
            .Include(t => t.MaPhongNavigation)
            .Where(t => t.DangHoatDong);

        if (maPhong.HasValue)
            query = query.Where(t => t.MaPhong == maPhong.Value);

        var data = await query
            .Select(t => new
            {
                t.MaId,
                t.MaPhong,
                SoPhong = t.MaPhongNavigation.SoPhong,
                t.TenTaiSan,
                t.LoaiTaiSan,
                t.SoLuong,
                t.GiaDenBu,
                t.GhiChu
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("hu-hong")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachHuHong()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var data = await db.MatMatHuHongs
            .AsNoTracking()
            .Include(m => m.MaChiTietDatPhongNavigation).ThenInclude(ct => ct.MaPhongNavigation)
            .Include(m => m.MaChiTietDatPhongNavigation).ThenInclude(ct => ct.MaDatPhongNavigation)
            .Include(m => m.MaTaiSanPhongNavigation)
            .OrderByDescending(m => m.NgayTao)
            .Take(50)
            .Select(m => new
            {
                m.MaId,
                m.MaChiTietDatPhong,
                MaDatPhong = m.MaChiTietDatPhongNavigation.MaDatPhongNavigation.MaDatPhong,
                TenKhach = m.MaChiTietDatPhongNavigation.MaDatPhongNavigation.TenKhach,
                SoPhong = m.MaChiTietDatPhongNavigation.MaPhongNavigation != null ? m.MaChiTietDatPhongNavigation.MaPhongNavigation.SoPhong : "N/A",
                TenTaiSan = m.MaTaiSanPhongNavigation != null ? m.MaTaiSanPhongNavigation.TenTaiSan : m.MoTa,
                m.SoLuong,
                m.TienPhat,
                m.MoTa,
                m.NgayTao
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost("hu-hong")]
    public async Task<ActionResult<ApiResponse<object>>> GhiNhanHuHong([FromBody] GhiNhanHuHongRequest req)
    {
        if (!User.HasPermission(Quyen.ManageHousekeeping) && !User.HasPermission(Quyen.CheckInOut))
            return Forbid();

        var ct = await db.ChiTietDatPhongs
            .Include(x => x.MaDatPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaId == req.MaChiTietDatPhong);

        if (ct == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy chi tiết đặt phòng."));

        decimal tienPhat = req.TienPhat;
        if (tienPhat <= 0 && req.MaTaiSanPhong.HasValue)
        {
            var taiSan = await db.TaiSanPhongs.FindAsync(req.MaTaiSanPhong.Value);
            if (taiSan != null)
                tienPhat = taiSan.GiaDenBu * req.SoLuong;
        }

        var huHong = new MatMatHuHong
        {
            MaChiTietDatPhong = req.MaChiTietDatPhong,
            MaTaiSanPhong = req.MaTaiSanPhong,
            SoLuong = req.SoLuong,
            TienPhat = tienPhat,
            MoTa = req.MoTa,
            DuongDanAnh = req.DuongDanAnh,
            NgayTao = DateTime.UtcNow
        };
        db.MatMatHuHongs.Add(huHong);

        // Tự động đồng bộ tiền phạt đền bù vào phụ thu Hóa đơn của đơn đặt phòng
        var hd = await db.HoaDons.FirstOrDefaultAsync(h => h.MaDatPhong == ct.MaDatPhong);
        if (hd != null && tienPhat > 0)
        {
            hd.TienPhuThu += tienPhat;
            hd.LyDoPhuThu = string.IsNullOrWhiteSpace(hd.LyDoPhuThu)
                ? $"Đền bù hư hỏng: {req.MoTa}"
                : $"{hd.LyDoPhuThu}; Đền bù: {req.MoTa}";
            hd.TongThanhToan = Math.Max(0, hd.TongTienPhong + hd.TongTienDichVu + hd.TienPhuThu + hd.TienThue - hd.TienGiamGia);
            if (StatusMapper.IsInvoicePaid(hd.TrangThai))
                hd.TrangThai = "Pending";
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "GHI_NHAN_HU_HONG",
            doiTuong: "MatMatHuHong",
            khoaChinh: huHong.MaId.ToString(),
            noiDung: $"Ghi nhận hư hỏng cho CTĐP #{req.MaChiTietDatPhong}: {req.MoTa}. Tiền phạt: {tienPhat:N0} VNĐ (đã cộng vào Hóa đơn)",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(new { huHong.MaId, huHong.TienPhat }, "Đã ghi nhận mất mát/hư hỏng và đồng bộ phụ thu vào hóa đơn."));
    }
}
