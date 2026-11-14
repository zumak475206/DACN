using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.DichVu;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Route("api/v1/dich-vu")]
[ApiController]
public class DichVuController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DichVuDto>>>> DanhSach()
    {
        var data = await db.DichVus
            .AsNoTracking()
            .Include(d => d.MaDanhMucDichVuNavigation)
            .Where(d => d.DangHoatDong)
            .Select(d => new DichVuDto
            {
                MaId = d.MaId,
                TenDanhMuc = d.MaDanhMucDichVuNavigation.Ten,
                Ten = d.Ten, GiaDichVu = d.GiaDichVu,
                DonViTinh = d.DonViTinh, MoTa = d.MoTa,
                DangHoatDong = d.DangHoatDong
            })
            .ToListAsync();
        return Ok(ApiResponse<List<DichVuDto>>.Ok(data));
    }

    [Authorize]
    [HttpPost("dat")]
    public async Task<ActionResult<ApiResponse<DonDichVuDto>>> DatDichVu([FromBody] DatDichVuRequest req)
    {
        var chiTietDatPhong = await db.ChiTietDatPhongs
            .Include(ct => ct.MaDatPhongNavigation)
            .FirstOrDefaultAsync(ct => ct.MaId == req.MaChiTietDatPhong);

        if (chiTietDatPhong == null)
            return NotFound(ApiResponse<DonDichVuDto>.Fail("Không tìm thấy chi tiết đặt phòng."));

        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && chiTietDatPhong.MaDatPhongNavigation?.MaNguoiDung != currentUserId)
            return Forbid();

        if (req.DanhSachDichVu == null || !req.DanhSachDichVu.Any())
            return BadRequest(ApiResponse<DonDichVuDto>.Fail("Phải chọn ít nhất một dịch vụ."));

        decimal tongTien = 0;
        var chiTietList = new List<(int MaDichVu, int SoLuong, decimal DonGia, decimal ThanhTien)>();

        foreach (var item in req.DanhSachDichVu)
        {
            if (item.SoLuong <= 0)
                return BadRequest(ApiResponse<DonDichVuDto>.Fail("Số lượng dịch vụ phải lớn hơn 0."));

            var dv = await db.DichVus.FindAsync(item.MaDichVu);
            if (dv == null || !dv.DangHoatDong)
                return BadRequest(ApiResponse<DonDichVuDto>.Fail($"Dịch vụ #{item.MaDichVu} không tồn tại hoặc đã ngừng cung cấp."));

            var thanhTien = dv.GiaDichVu * item.SoLuong;
            tongTien += thanhTien;
            chiTietList.Add((item.MaDichVu, item.SoLuong, dv.GiaDichVu, thanhTien));
        }

        var don = new DonDichVu
        {
            MaChiTietDatPhong = req.MaChiTietDatPhong,
            TrangThai = "Pending",
            TongTien = tongTien,
            NgayDatDichVu = DateTime.UtcNow
        };
        db.DonDichVus.Add(don);
        await db.SaveChangesAsync();

        foreach (var (maDichVu, soLuong, donGia, _) in chiTietList)
        {
            db.ChiTietDonDichVus.Add(new ChiTietDonDichVu
            {
                MaDonDichVu = don.MaId,
                MaDichVu = maDichVu,
                SoLuong = soLuong,
                DonGia = donGia
            });
        }
        await db.SaveChangesAsync();

        var hoaDon = await db.HoaDons.FirstOrDefaultAsync(h => h.MaDatPhong == chiTietDatPhong.MaDatPhong);
        if (hoaDon != null)
        {
            hoaDon.TongTienDichVu += tongTien;
            hoaDon.TongThanhToan += tongTien;
            if (StatusMapper.IsInvoicePaid(hoaDon.TrangThai))
                hoaDon.TrangThai = "Pending";

            await db.SaveChangesAsync();
        }

        await auditService.GhiNhanAsync(
            hanhDong: "DAT_DICH_VU",
            doiTuong: "DonDichVu",
            khoaChinh: don.MaId.ToString(),
            noiDung: $"Đặt dịch vụ cho chi tiết đặt phòng #{req.MaChiTietDatPhong}. Tổng tiền dịch vụ: {tongTien:N0} VNĐ (Đã đồng bộ vào Hóa đơn)",
            maNguoiDung: currentUserId,
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        var donLoaded = await db.DonDichVus
            .Include(d => d.ChiTietDonDichVus).ThenInclude(ct => ct.MaDichVuNavigation)
            .FirstAsync(d => d.MaId == don.MaId);

        return Ok(ApiResponse<DonDichVuDto>.Ok(MapDonDichVu(donLoaded), "Đặt dịch vụ thành công. Tiền dịch vụ đã được cập nhật vào hóa đơn."));
    }

    [Authorize]
    [HttpGet("don/{maDonDichVu:int}")]
    public async Task<ActionResult<ApiResponse<DonDichVuDto>>> ChiTietDon(int maDonDichVu)
    {
        var don = await db.DonDichVus
            .AsNoTracking()
            .Include(d => d.ChiTietDonDichVus).ThenInclude(ct => ct.MaDichVuNavigation)
            .Include(d => d.MaChiTietDatPhongNavigation).ThenInclude(ct => ct.MaDatPhongNavigation)
            .FirstOrDefaultAsync(d => d.MaId == maDonDichVu);

        if (don == null) return NotFound(ApiResponse<DonDichVuDto>.Fail("Không tìm thấy đơn dịch vụ."));

        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && don.MaChiTietDatPhongNavigation?.MaDatPhongNavigation?.MaNguoiDung != currentUserId)
            return Forbid();

        return Ok(ApiResponse<DonDichVuDto>.Ok(MapDonDichVu(don)));
    }

    private static DonDichVuDto MapDonDichVu(DonDichVu d) => new()
    {
        MaId = d.MaId, MaChiTietDatPhong = d.MaChiTietDatPhong,
        TrangThai = d.TrangThai, TongTien = d.TongTien, NgayTao = d.NgayDatDichVu,
        ChiTiet = d.ChiTietDonDichVus.Select(ct => new ChiTietDonDichVuDto
        {
            MaDichVu = ct.MaDichVu, TenDichVu = ct.MaDichVuNavigation?.Ten ?? "",
            SoLuong = ct.SoLuong, DonGia = ct.DonGia,
            ThanhTien = ct.DonGia * ct.SoLuong
        }).ToList()
    };
}
