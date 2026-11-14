using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.Voucher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Route("api/v1/voucher")]
[ApiController]
public class VoucherController(DACNContext db) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<VoucherDto>>>> DanhSach()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var data = await db.MaGiamGia.AsNoTracking().Select(v => new VoucherDto
        {
            MaId = v.MaId, MaCode = v.MaCode, LoaiGiamGia = v.LoaiGiamGia,
            GiaTriGiamGia = v.GiaTriGiamGia, GiaTriDatPhongToiThieu = v.GiaTriDatPhongToiThieu,
            HieuLucTu = v.HieuLucTu, HieuLucDen = v.HieuLucDen,
            GioiHanSuDung = v.GioiHanSuDung, SoLanDaDung = v.SoLanDaDung,
            DangHoatDong = v.DangHoatDong
        }).ToListAsync();
        return Ok(ApiResponse<List<VoucherDto>>.Ok(data));
    }

    [AllowAnonymous]
    [HttpPost("kiem-tra")]
    public async Task<ActionResult<ApiResponse<KiemTraVoucherResponse>>> KiemTra([FromBody] KiemTraVoucherRequest req)
    {
        var v = await db.MaGiamGia.FirstOrDefaultAsync(x =>
            x.MaCode == req.MaCode && x.DangHoatDong
            && x.HieuLucTu <= DateTime.Now && x.HieuLucDen >= DateTime.Now
            && x.SoLanDaDung < x.GioiHanSuDung);

        if (v == null)
            return Ok(ApiResponse<KiemTraVoucherResponse>.Ok(new KiemTraVoucherResponse
            { HopLe = false, TienGiam = 0, ThongBao = "Mã giảm giá không hợp lệ hoặc đã hết lượt sử dụng." }));

        if (v.GiaTriDatPhongToiThieu.HasValue && req.TongTienDatPhong < v.GiaTriDatPhongToiThieu.Value)
            return Ok(ApiResponse<KiemTraVoucherResponse>.Ok(new KiemTraVoucherResponse
            { HopLe = false, TienGiam = 0, ThongBao = $"Đơn hàng phải đạt tối thiểu {v.GiaTriDatPhongToiThieu:N0} VNĐ." }));

        var tienGiam = StatusMapper.IsPercentDiscount(v.LoaiGiamGia)
            ? req.TongTienDatPhong * v.GiaTriGiamGia / 100
            : v.GiaTriGiamGia;

        return Ok(ApiResponse<KiemTraVoucherResponse>.Ok(new KiemTraVoucherResponse
        { HopLe = true, TienGiam = tienGiam, ThongBao = $"Áp dụng thành công! Giảm {tienGiam:N0} VNĐ." }));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Tao([FromBody] TaoVoucherRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        if (await db.MaGiamGia.AnyAsync(v => v.MaCode == req.MaCode))
            return BadRequest(ApiResponse<VoucherDto>.Fail("Mã code đã tồn tại."));

        var v = new MaGiamGium
        {
            MaCode = req.MaCode,
            LoaiGiamGia = StatusMapper.NormalizeLoaiGiamGia(req.LoaiGiamGia),
            GiaTriGiamGia = req.GiaTriGiamGia,
            GiaTriDatPhongToiThieu = req.GiaTriDatPhongToiThieu,
            HieuLucTu = req.HieuLucTu,
            HieuLucDen = req.HieuLucDen,
            GioiHanSuDung = req.GioiHanSuDung,
            SoLanDaDung = 0,
            DangHoatDong = true,
            NgayTao = DateTime.UtcNow
        };
        db.MaGiamGia.Add(v);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<VoucherDto>.Ok(new VoucherDto
        { MaId = v.MaId, MaCode = v.MaCode, LoaiGiamGia = v.LoaiGiamGia,
          GiaTriGiamGia = v.GiaTriGiamGia, DangHoatDong = true }, "Tạo voucher thành công."));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhat(int id, [FromBody] TaoVoucherRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var v = await db.MaGiamGia.FindAsync(id);
        if (v == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy voucher."));
        v.LoaiGiamGia = StatusMapper.NormalizeLoaiGiamGia(req.LoaiGiamGia);
        v.GiaTriGiamGia = req.GiaTriGiamGia;
        v.GiaTriDatPhongToiThieu = req.GiaTriDatPhongToiThieu;
        v.HieuLucTu = req.HieuLucTu;
        v.HieuLucDen = req.HieuLucDen;
        v.GioiHanSuDung = req.GioiHanSuDung;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật voucher thành công."));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> VoHieuHoa(int id)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var v = await db.MaGiamGia.FindAsync(id);
        if (v == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy voucher."));
        v.DangHoatDong = false;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã vô hiệu hóa voucher."));
    }
}
