using System.Security.Claims;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.Voucher;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Authorize]
[Route("api/v1/voucher")]
[ApiController]
public class VoucherController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<VoucherDto>>>> DanhSach()
    {
        if (!User.HasPermission(Quyen.ManageVouchers))
            return Forbid();

        var data = await db.MaGiamGia.AsNoTracking()
            .OrderByDescending(v => v.NgayTao)
            .Select(v => new VoucherDto
            {
                MaId = v.MaId,
                MaCode = v.MaCode,
                LoaiGiamGia = v.LoaiGiamGia,
                GiaTriGiamGia = v.GiaTriGiamGia,
                GiaTriDatPhongToiThieu = v.GiaTriDatPhongToiThieu,
                GiamToiDa = v.GiamToiDa,
                HieuLucTu = v.HieuLucTu,
                HieuLucDen = v.HieuLucDen,
                GioiHanSuDung = v.GioiHanSuDung,
                SoLanDaDung = v.SoLanDaDung,
                DangHoatDong = v.DangHoatDong
            }).ToListAsync();

        return Ok(ApiResponse<List<VoucherDto>>.Ok(data));
    }

    [AllowAnonymous]
    [HttpGet("cong-khai")]
    public async Task<ActionResult<ApiResponse<List<VoucherCongKhaiDto>>>> DanhSachCongKhai()
    {
        var now = GioVN.Now;
        var data = await db.MaGiamGia.AsNoTracking()
            .Where(v => v.DangHoatDong && v.HieuLucTu <= now && v.HieuLucDen >= now && v.SoLanDaDung < v.GioiHanSuDung)
            .OrderBy(v => v.HieuLucDen)
            .Select(v => new VoucherCongKhaiDto
            {
                MaCode = v.MaCode,
                LoaiGiamGia = v.LoaiGiamGia,
                GiaTriGiamGia = v.GiaTriGiamGia,
                GiaTriDatPhongToiThieu = v.GiaTriDatPhongToiThieu,
                GiamToiDa = v.GiamToiDa,
                HieuLucDen = v.HieuLucDen,
                SoLuotConLai = v.GioiHanSuDung - v.SoLanDaDung
            }).ToListAsync();

        return Ok(ApiResponse<List<VoucherCongKhaiDto>>.Ok(data));
    }

    [AllowAnonymous]
    [EnableRateLimiting("public-strict")]
    [HttpPost("kiem-tra")]
    public async Task<ActionResult<ApiResponse<KiemTraVoucherResponse>>> KiemTra([FromBody] KiemTraVoucherRequest req)
    {
        var code = (req.MaCode ?? string.Empty).Trim().ToUpperInvariant();
        var now = GioVN.Now;

        var v = await db.MaGiamGia.FirstOrDefaultAsync(x =>
            x.MaCode.ToUpper() == code && x.DangHoatDong
            && x.HieuLucTu <= now && x.HieuLucDen >= now
            && x.SoLanDaDung < x.GioiHanSuDung);

        if (v == null)
            return Ok(ApiResponse<KiemTraVoucherResponse>.Ok(new KiemTraVoucherResponse
            { HopLe = false, TienGiam = 0, ThongBao = "Mã giảm giá không hợp lệ hoặc đã hết lượt sử dụng." }));

        if (v.GiaTriDatPhongToiThieu.HasValue && req.TongTienDatPhong < v.GiaTriDatPhongToiThieu.Value)
            return Ok(ApiResponse<KiemTraVoucherResponse>.Ok(new KiemTraVoucherResponse
            { HopLe = false, TienGiam = 0, ThongBao = $"Đơn hàng phải đạt tối thiểu {v.GiaTriDatPhongToiThieu:N0} VNĐ." }));

        var tienGiam = StatusMapper.IsPercentDiscount(v.LoaiGiamGia)
            ? req.TongTienDatPhong * v.GiaTriGiamGia / 100m
            : v.GiaTriGiamGia;

        if (StatusMapper.IsPercentDiscount(v.LoaiGiamGia) && v.GiamToiDa.HasValue && v.GiamToiDa.Value > 0 && tienGiam > v.GiamToiDa.Value)
            tienGiam = v.GiamToiDa.Value;

        if (tienGiam > req.TongTienDatPhong)
            tienGiam = req.TongTienDatPhong;

        return Ok(ApiResponse<KiemTraVoucherResponse>.Ok(new KiemTraVoucherResponse
        { HopLe = true, TienGiam = tienGiam, ThongBao = $"Áp dụng thành công! Giảm {tienGiam:N0} VNĐ." }));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Tao([FromBody] TaoVoucherRequest req)
    {
        if (!User.HasPermission(Quyen.ManageVouchers))
            return Forbid();

        var normalizedCode = req.MaCode.Trim().ToUpperInvariant().Replace(" ", "");
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return BadRequest(ApiResponse<VoucherDto>.Fail("Mã voucher không hợp lệ."));

        if (req.HieuLucTu >= req.HieuLucDen)
            return BadRequest(ApiResponse<VoucherDto>.Fail("Ngày bắt đầu hiệu lực phải trước ngày kết thúc."));

        var loai = StatusMapper.NormalizeLoaiGiamGia(req.LoaiGiamGia);
        if (StatusMapper.IsPercentDiscount(loai) && (req.GiaTriGiamGia <= 0 || req.GiaTriGiamGia > 100))
            return BadRequest(ApiResponse<VoucherDto>.Fail("Voucher giảm theo phần trăm phải có giá trị từ 1% đến 100%."));

        if (await db.MaGiamGia.AnyAsync(v => v.MaCode.ToUpper() == normalizedCode))
            return BadRequest(ApiResponse<VoucherDto>.Fail("Mã code đã tồn tại."));

        var v = new MaGiamGium
        {
            MaCode = normalizedCode,
            LoaiGiamGia = loai,
            GiaTriGiamGia = req.GiaTriGiamGia,
            GiaTriDatPhongToiThieu = req.GiaTriDatPhongToiThieu,
            GiamToiDa = req.GiamToiDa,
            HieuLucTu = req.HieuLucTu,
            HieuLucDen = req.HieuLucDen,
            GioiHanSuDung = req.GioiHanSuDung,
            SoLanDaDung = 0,
            DangHoatDong = true,
            NgayTao = DateTime.UtcNow
        };
        db.MaGiamGia.Add(v);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "TAO_VOUCHER",
            doiTuong: "MaGiamGia",
            khoaChinh: v.MaId.ToString(),
            noiDung: $"Tạo mã giảm giá '{v.MaCode}' ({v.LoaiGiamGia}: {v.GiaTriGiamGia:N0})",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<VoucherDto>.Ok(new VoucherDto
        {
            MaId = v.MaId,
            MaCode = v.MaCode,
            LoaiGiamGia = v.LoaiGiamGia,
            GiaTriGiamGia = v.GiaTriGiamGia,
            GiaTriDatPhongToiThieu = v.GiaTriDatPhongToiThieu,
            GiamToiDa = v.GiamToiDa,
            HieuLucTu = v.HieuLucTu,
            HieuLucDen = v.HieuLucDen,
            GioiHanSuDung = v.GioiHanSuDung,
            DangHoatDong = true
        }, "Tạo voucher thành công."));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhat(int id, [FromBody] TaoVoucherRequest req)
    {
        if (!User.HasPermission(Quyen.ManageVouchers))
            return Forbid();

        if (req.HieuLucTu >= req.HieuLucDen)
            return BadRequest(ApiResponse<object>.Fail("Ngày bắt đầu hiệu lực phải trước ngày kết thúc."));

        var loai = StatusMapper.NormalizeLoaiGiamGia(req.LoaiGiamGia);
        if (StatusMapper.IsPercentDiscount(loai) && (req.GiaTriGiamGia <= 0 || req.GiaTriGiamGia > 100))
            return BadRequest(ApiResponse<object>.Fail("Voucher giảm theo phần trăm phải có giá trị từ 1% đến 100%."));

        var v = await db.MaGiamGia.FindAsync(id);
        if (v == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy voucher."));

        if (req.GioiHanSuDung < v.SoLanDaDung)
            return BadRequest(ApiResponse<object>.Fail($"Giới hạn sử dụng không được nhỏ hơn số lần đã dùng ({v.SoLanDaDung})."));

        v.LoaiGiamGia = loai;
        v.GiaTriGiamGia = req.GiaTriGiamGia;
        v.GiaTriDatPhongToiThieu = req.GiaTriDatPhongToiThieu;
        v.GiamToiDa = req.GiamToiDa;
        v.HieuLucTu = req.HieuLucTu;
        v.HieuLucDen = req.HieuLucDen;
        v.GioiHanSuDung = req.GioiHanSuDung;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_VOUCHER",
            doiTuong: "MaGiamGia",
            khoaChinh: v.MaId.ToString(),
            noiDung: $"Cập nhật mã giảm giá '{v.MaCode}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật voucher thành công."));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> VoHieuHoa(int id)
    {
        if (!User.HasPermission(Quyen.ManageVouchers))
            return Forbid();

        var v = await db.MaGiamGia.FindAsync(id);
        if (v == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy voucher."));
        v.DangHoatDong = false;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "VO_HIEU_HOA_VOUCHER",
            doiTuong: "MaGiamGia",
            khoaChinh: v.MaId.ToString(),
            noiDung: $"Vô hiệu hóa mã giảm giá '{v.MaCode}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đã vô hiệu hóa voucher."));
    }
}
