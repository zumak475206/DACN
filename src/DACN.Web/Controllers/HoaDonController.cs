using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.HoaDon;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

[Authorize]
[Route("api/v1/hoa-don")]
[ApiController]
public class HoaDonController(DACNContext db, IConfiguration config, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<HoaDonDto>>>> DanhSach([FromQuery] string? trangThai = null)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var query = db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.ThanhToans)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
        {
            var normalized = StatusMapper.NormalizeHoaDonTrangThai(trangThai);
            query = query.Where(h => h.TrangThai == trangThai || h.TrangThai == normalized);
        }

        var list = await query.OrderByDescending(h => h.NgayTao).Take(100).ToListAsync();
        return Ok(ApiResponse<List<HoaDonDto>>.Ok(list.Select(MapHoaDon).ToList()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<HoaDonDto>>> ChiTiet(int id)
    {
        var hd = await db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.ThanhToans)
            .FirstOrDefaultAsync(h => h.MaId == id);

        if (hd == null) return NotFound(ApiResponse<HoaDonDto>.Fail("Không tìm thấy hóa đơn."));

        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && hd.MaDatPhongNavigation?.MaNguoiDung != currentUserId)
            return Forbid();

        return Ok(ApiResponse<HoaDonDto>.Ok(MapHoaDon(hd)));
    }

    [HttpGet("dat-phong/{maDatPhong:int}")]
    public async Task<ActionResult<ApiResponse<HoaDonDto>>> TheoDatPhong(int maDatPhong)
    {
        var hd = await db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.ThanhToans)
            .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong);

        if (hd == null) return NotFound(ApiResponse<HoaDonDto>.Fail("Chưa có hóa đơn cho đặt phòng này."));

        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && hd.MaDatPhongNavigation?.MaNguoiDung != currentUserId)
            return Forbid();

        return Ok(ApiResponse<HoaDonDto>.Ok(MapHoaDon(hd)));
    }

    [HttpPut("{id:int}/phu-thu")]
    public async Task<ActionResult<ApiResponse<HoaDonDto>>> CapNhatPhuThu(int id, [FromBody] CapNhatPhuThuRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var hd = await db.HoaDons
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.ThanhToans)
            .FirstOrDefaultAsync(h => h.MaId == id);

        if (hd == null) return NotFound(ApiResponse<HoaDonDto>.Fail("Không tìm thấy hóa đơn."));

        hd.TienPhuThu = req.TienPhuThu;
        hd.LyDoPhuThu = req.LyDoPhuThu;
        hd.TongThanhToan = Math.Max(0, hd.TongTienPhong + hd.TongTienDichVu + hd.TienPhuThu + hd.TienThue - hd.TienGiamGia);

        var tongDaTra = hd.ThanhToans.Where(t => StatusMapper.IsPaymentSuccess(t.TrangThai)).Sum(t => t.SoTienDaThanhToan);
        hd.TrangThai = tongDaTra >= hd.TongThanhToan ? "Paid" : "Pending";

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_PHU_THU_HOA_DON",
            doiTuong: "HoaDon",
            khoaChinh: hd.MaId.ToString(),
            noiDung: $"Cập nhật phụ thu HĐ #{hd.MaId}: {req.TienPhuThu:N0} VNĐ ({req.LyDoPhuThu}). Tổng thanh toán mới: {hd.TongThanhToan:N0} VNĐ",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<HoaDonDto>.Ok(MapHoaDon(hd), "Cập nhật phụ thu hóa đơn thành công."));
    }

    [HttpPost("thanh-toan")]
    public async Task<ActionResult<ApiResponse<HoaDonDto>>> ThanhToan([FromBody] TaoThanhToanRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var hd = await db.HoaDons
            .Include(h => h.ThanhToans)
            .FirstOrDefaultAsync(h => h.MaId == req.MaHoaDon);

        if (hd == null) return NotFound(ApiResponse<HoaDonDto>.Fail("Không tìm thấy hóa đơn."));

        if (req.SoTienDaThanhToan <= 0)
            return BadRequest(ApiResponse<HoaDonDto>.Fail("Số tiền thanh toán phải lớn hơn 0."));

        var daTra = hd.ThanhToans.Where(t => StatusMapper.IsPaymentSuccess(t.TrangThai)).Sum(t => t.SoTienDaThanhToan);
        var conNo = hd.TongThanhToan - daTra;
        if (conNo <= 0 && StatusMapper.IsInvoicePaid(hd.TrangThai))
            return BadRequest(ApiResponse<HoaDonDto>.Fail("Hóa đơn này đã được thanh toán đủ."));

        var currentUserId = User.GetUserId();
        hd.NguoiLapHoaDon = currentUserId;

        var phuongThuc = StatusMapper.NormalizePhuongThucThanhToan(req.PhuongThucThanhToan);
        var tt = new ThanhToan
        {
            MaHoaDon = req.MaHoaDon,
            PhuongThucThanhToan = phuongThuc,
            SoTienDaThanhToan = req.SoTienDaThanhToan,
            MaGiaoDich = req.MaGiaoDich ?? $"PAY-{DateTime.UtcNow:yyMMddHHmmss}",
            TrangThai = "Success",
            NgayThanhToan = DateTime.UtcNow
        };
        db.ThanhToans.Add(tt);

        var tongDaThanhToan = daTra + req.SoTienDaThanhToan;
        if (tongDaThanhToan >= hd.TongThanhToan)
            hd.TrangThai = "Paid";
        else
            hd.TrangThai = "Pending";

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "THANH_TOAN",
            doiTuong: "HoaDon",
            khoaChinh: req.MaHoaDon.ToString(),
            noiDung: $"Thanh toán {req.SoTienDaThanhToan:N0} VNĐ qua {phuongThuc}. Mã GD: {tt.MaGiaoDich}. Trạng thái HĐ: {hd.TrangThai}",
            maNguoiDung: currentUserId,
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        var hdUpdated = await db.HoaDons
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.ThanhToans)
            .FirstAsync(h => h.MaId == req.MaHoaDon);

        return Ok(ApiResponse<HoaDonDto>.Ok(MapHoaDon(hdUpdated), "Thanh toán thành công."));
    }

    [AllowAnonymous]
    [HttpPost("webhook-thanh-toan")]
    public async Task<ActionResult<ApiResponse<object>>> WebhookThanhToan([FromBody] WebhookThanhToanRequest req)
    {
        var secret = Environment.GetEnvironmentVariable("DACN_PAYMENT_WEBHOOK_SECRET")
            ?? config["Payment:WebhookSecret"]
            ?? "DACN_VNPay_MoMo_Webhook_Secret_2026";

        var payload = $"{req.MaHoaDon}|{req.SoTien:0.##}|{req.MaGiaoDich}|{req.PhuongThuc}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedSig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSig),
                Encoding.UTF8.GetBytes(req.ChuKy.Trim().ToLowerInvariant())))
        {
            return Unauthorized(ApiResponse<object>.Fail("Chữ ký HMAC xác thực cổng thanh toán không hợp lệ."));
        }

        if (req.SoTien <= 0)
            return BadRequest(ApiResponse<object>.Fail("Số tiền không hợp lệ."));

        var hd = await db.HoaDons.Include(h => h.ThanhToans).FirstOrDefaultAsync(h => h.MaId == req.MaHoaDon);
        if (hd == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy hóa đơn."));

        if (hd.ThanhToans.Any(t => t.MaGiaoDich == req.MaGiaoDich))
            return Ok(ApiResponse<object>.Ok(null!, "Giao dịch đã được ghi nhận trước đó (Idempotent)."));

        var phuongThuc = StatusMapper.NormalizePhuongThucThanhToan(req.PhuongThuc);
        db.ThanhToans.Add(new ThanhToan
        {
            MaHoaDon = hd.MaId,
            PhuongThucThanhToan = phuongThuc,
            SoTienDaThanhToan = req.SoTien,
            MaGiaoDich = req.MaGiaoDich,
            TrangThai = "Success",
            NgayThanhToan = DateTime.UtcNow
        });

        var daTra = hd.ThanhToans.Where(t => StatusMapper.IsPaymentSuccess(t.TrangThai)).Sum(t => t.SoTienDaThanhToan) + req.SoTien;
        if (daTra >= hd.TongThanhToan)
            hd.TrangThai = "Paid";

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "WEBHOOK_THANH_TOAN",
            doiTuong: "HoaDon",
            khoaChinh: hd.MaId.ToString(),
            noiDung: $"Webhook {phuongThuc} xác nhận thanh toán {req.SoTien:N0} VNĐ (Mã GD: {req.MaGiaoDich})",
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Ghi nhận thanh toán từ cổng trực tuyến thành công."));
    }

    private static HoaDonDto MapHoaDon(HoaDon hd) => new()
    {
        MaId = hd.MaId, MaDatPhong = hd.MaDatPhong,
        MaDatPhongCode = hd.MaDatPhongNavigation?.MaDatPhong ?? "",
        TenKhach = hd.MaDatPhongNavigation?.TenKhach,
        TongTienPhong = hd.TongTienPhong, TongTienDichVu = hd.TongTienDichVu,
        TienGiamGia = hd.TienGiamGia, TienThue = hd.TienThue,
        TienPhuThu = hd.TienPhuThu, LyDoPhuThu = hd.LyDoPhuThu,
        TongThanhToan = hd.TongThanhToan, TrangThai = hd.TrangThai,
        NgayTao = hd.NgayTao,
        DanhSachThanhToan = hd.ThanhToans.Select(t => new ThanhToanDto
        {
            MaId = t.MaId, PhuongThucThanhToan = t.PhuongThucThanhToan,
            SoTienDaThanhToan = t.SoTienDaThanhToan, MaGiaoDich = t.MaGiaoDich,
            TrangThai = t.TrangThai, NgayThanhToan = t.NgayThanhToan
        }).ToList()
    };
}
