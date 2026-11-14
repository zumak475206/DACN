using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.DTOs.DatPhong;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DACN.Web.Controllers;

[Route("api/v1/dat-phong")]
[ApiController]
public class DatPhongController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<KetQuaPhanTrang<DatPhongDto>>>> DanhSach(
        [FromQuery] PhanTrangRequest req, [FromQuery] string? trangThai = null)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var query = db.DatPhongs
            .AsNoTracking()
            .Include(d => d.ChiTietDatPhongs).ThenInclude(ct => ct.MaLoaiPhongNavigation)
            .Include(d => d.ChiTietDatPhongs).ThenInclude(ct => ct.MaPhongNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
        {
            var normalized = StatusMapper.NormalizeDatPhongTrangThai(trangThai);
            query = query.Where(d => d.TrangThai == trangThai || d.TrangThai == normalized);
        }

        var total = await query.CountAsync();
        var rawData = await query
            .OrderByDescending(d => d.NgayTao)
            .Skip((req.Trang - 1) * req.SoLuong)
            .Take(req.SoLuong)
            .ToListAsync();
        var data = rawData.Select(MapDatPhong).ToList();

        return Ok(ApiResponse<KetQuaPhanTrang<DatPhongDto>>.Ok(
            new KetQuaPhanTrang<DatPhongDto> { DuLieu = data, TongSo = total, Trang = req.Trang, SoLuong = req.SoLuong }));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DatPhongDto>>> ChiTiet(int id)
    {
        var d = await db.DatPhongs
            .AsNoTracking()
            .Include(x => x.ChiTietDatPhongs).ThenInclude(ct => ct.MaLoaiPhongNavigation)
            .Include(x => x.ChiTietDatPhongs).ThenInclude(ct => ct.MaPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaId == id);

        if (d == null) return NotFound(ApiResponse<DatPhongDto>.Fail("Không tìm thấy đặt phòng."));

        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && d.MaNguoiDung != currentUserId)
            return Forbid();

        return Ok(ApiResponse<DatPhongDto>.Ok(MapDatPhong(d)));
    }

    [Authorize]
    [HttpGet("cua-toi")]
    public async Task<ActionResult<ApiResponse<List<DatPhongDto>>>> CuaToi()
    {
        var currentUserId = User.GetUserId();
        if (!currentUserId.HasValue)
            return Unauthorized(ApiResponse<List<DatPhongDto>>.Fail("Vui lòng đăng nhập."));

        var rawData = await db.DatPhongs
            .AsNoTracking()
            .Include(d => d.ChiTietDatPhongs).ThenInclude(ct => ct.MaLoaiPhongNavigation)
            .Include(d => d.ChiTietDatPhongs).ThenInclude(ct => ct.MaPhongNavigation)
            .Where(d => d.MaNguoiDung == currentUserId.Value)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync();
        var data = rawData.Select(MapDatPhong).ToList();

        return Ok(ApiResponse<List<DatPhongDto>>.Ok(data));
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DatPhongDto>>> TaoDatPhong([FromBody] TaoDatPhongRequest req)
    {
        if (req.DanhSachPhong == null || !req.DanhSachPhong.Any())
            return BadRequest(ApiResponse<DatPhongDto>.Fail("Phải chọn ít nhất một loại phòng."));

        var today = DateTime.UtcNow.Date;
        foreach (var item in req.DanhSachPhong)
        {
            if (item.NgayNhanPhong.Date < today)
                return BadRequest(ApiResponse<DatPhongDto>.Fail("Ngày nhận phòng không thể ở trong quá khứ."));

            if (item.NgayTraPhong.Date <= item.NgayNhanPhong.Date)
                return BadRequest(ApiResponse<DatPhongDto>.Fail("Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 đêm."));
        }

        var isRelational = db.Database.IsRelational();
        IDbContextTransaction? tx = isRelational
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        try
        {
            decimal tongTien = 0;
            decimal tienGiam = 0;
            var now = DateTime.UtcNow;

            MaGiamGium? voucher = null;
            if (!string.IsNullOrEmpty(req.MaGiamGia))
            {
                voucher = await db.MaGiamGia.FirstOrDefaultAsync(v =>
                    v.MaCode == req.MaGiamGia && v.DangHoatDong
                    && v.HieuLucTu <= DateTime.Now && v.HieuLucDen >= DateTime.Now
                    && v.SoLanDaDung < v.GioiHanSuDung);

                if (voucher == null)
                    return BadRequest(ApiResponse<DatPhongDto>.Fail("Mã giảm giá không hợp lệ hoặc đã hết lượt sử dụng."));
            }

            var chiTietList = new List<ChiTietDatPhong>();
            foreach (var item in req.DanhSachPhong)
            {
                var loaiPhong = await db.LoaiPhongs.FindAsync(item.MaLoaiPhong);
                if (loaiPhong == null || !loaiPhong.DangHoatDong)
                    return BadRequest(ApiResponse<DatPhongDto>.Fail($"Loại phòng #{item.MaLoaiPhong} không tồn tại hoặc đã ngừng phục vụ."));

                var soNgay = (item.NgayTraPhong.Date - item.NgayNhanPhong.Date).Days;

                var soPhongDangDung = await db.ChiTietDatPhongs.CountAsync(ct =>
                    ct.MaLoaiPhong == item.MaLoaiPhong
                    && ct.TrangThai != "Cancelled" && ct.TrangThai != "da_huy"
                    && ct.NgayNhanPhongDuKien < item.NgayTraPhong
                    && ct.NgayTraPhongDuKien > item.NgayNhanPhong);

                var soPhongCoSan = await db.Phongs.CountAsync(p =>
                    p.MaLoaiPhong == item.MaLoaiPhong
                    && p.TrangThai != "Maintenance" && p.TrangThai != "bao_duong");

                if (soPhongDangDung >= soPhongCoSan)
                {
                    if (tx != null) await tx.RollbackAsync();
                    return BadRequest(ApiResponse<DatPhongDto>.Fail($"Loại phòng '{loaiPhong.Ten}' đã hết phòng trống trong khoảng thời gian này."));
                }

                var giaPhong = loaiPhong.GiaCoBan * soNgay;
                tongTien += giaPhong;

                chiTietList.Add(new ChiTietDatPhong
                {
                    MaLoaiPhong = item.MaLoaiPhong,
                    NgayNhanPhongDuKien = item.NgayNhanPhong,
                    NgayTraPhongDuKien = item.NgayTraPhong,
                    GiaMoiDem = loaiPhong.GiaCoBan,
                    TrangThai = "Pending"
                });
            }

            if (voucher != null)
            {
                if (voucher.GiaTriDatPhongToiThieu.HasValue && tongTien < voucher.GiaTriDatPhongToiThieu.Value)
                    return BadRequest(ApiResponse<DatPhongDto>.Fail($"Đơn hàng phải đạt tối thiểu {voucher.GiaTriDatPhongToiThieu:N0} VNĐ để áp dụng mã giảm giá này."));

                tienGiam = StatusMapper.IsPercentDiscount(voucher.LoaiGiamGia)
                    ? tongTien * voucher.GiaTriGiamGia / 100
                    : voucher.GiaTriGiamGia;

                if (isRelational)
                {
                    var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE MaGiamGia SET SoLanDaDung = SoLanDaDung + 1 WHERE MaId = {voucher.MaId} AND SoLanDaDung < GioiHanSuDung AND DangHoatDong = 1");

                    if (rowsAffected == 0)
                    {
                        if (tx != null) await tx.RollbackAsync();
                        return BadRequest(ApiResponse<DatPhongDto>.Fail("Mã giảm giá vừa hết lượt sử dụng. Vui lòng thử lại."));
                    }
                }
                else
                {
                    voucher.SoLanDaDung += 1;
                }
            }

            var loggedInUserId = User.GetUserId();
            var maNguoiDung = loggedInUserId ?? req.MaNguoiDung;

            var maDatPhong = $"DP{now:yyMMddHHmmss}{RandomNumberGenerator.GetInt32(1000, 9999)}";
            var datPhong = new DatPhong
            {
                MaDatPhong = maDatPhong,
                MaNguoiDung = maNguoiDung,
                TenKhach = req.TenKhach,
                Sdtkhach = req.SdtKhach,
                EmailKhach = req.EmailKhach,
                MaGiamGia = voucher?.MaId,
                KenhDatPhong = StatusMapper.NormalizeKenhDatPhong(req.KenhDatPhong),
                TrangThai = "Pending",
                TongTien = Math.Max(0, tongTien - tienGiam),
                TienDatCoc = Math.Max(0, (tongTien - tienGiam) * 0.3m),
                GhiChu = req.GhiChu,
                NgayTao = now,
                NgayCapNhat = now
            };

            db.DatPhongs.Add(datPhong);
            await db.SaveChangesAsync();

            foreach (var ct in chiTietList)
            {
                ct.MaDatPhong = datPhong.MaId;
                db.ChiTietDatPhongs.Add(ct);
            }
            await db.SaveChangesAsync();

            var hoaDon = new HoaDon
            {
                MaDatPhong = datPhong.MaId,
                TongTienPhong = datPhong.TongTien,
                TongTienDichVu = 0,
                TienGiamGia = tienGiam,
                TienThue = 0,
                TienPhuThu = 0,
                TongThanhToan = datPhong.TongTien,
                TrangThai = "Pending",
                NgayTao = now
            };
            db.HoaDons.Add(hoaDon);
            await db.SaveChangesAsync();

            if (tx != null) await tx.CommitAsync();

            await auditService.GhiNhanAsync(
                hanhDong: "DAT_PHONG",
                doiTuong: "DatPhong",
                khoaChinh: datPhong.MaDatPhong,
                noiDung: $"Tạo đơn đặt phòng thành công. Khách: {datPhong.TenKhach}, SĐT: {datPhong.Sdtkhach}, Tổng tiền: {datPhong.TongTien:N0} VNĐ",
                maNguoiDung: loggedInUserId,
                email: datPhong.EmailKhach,
                diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
                thanhCong: true);

            var result = await db.DatPhongs
                .Include(d => d.ChiTietDatPhongs).ThenInclude(ct => ct.MaLoaiPhongNavigation)
                .FirstAsync(d => d.MaId == datPhong.MaId);

            return Ok(ApiResponse<DatPhongDto>.Ok(MapDatPhong(result), "Đặt phòng thành công!"));
        }
        catch (Exception)
        {
            if (tx != null) await tx.RollbackAsync();
            throw;
        }
    }

    [Authorize]
    [HttpPut("{id:int}/xac-nhan")]
    public async Task<ActionResult<ApiResponse<object>>> XacNhanDatPhong(int id, [FromQuery] bool daThuCoc = false)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var d = await db.DatPhongs.Include(x => x.ChiTietDatPhongs).FirstOrDefaultAsync(x => x.MaId == id);
        if (d == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đặt phòng."));

        if (StatusMapper.IsBookingCancelled(d.TrangThai))
            return BadRequest(ApiResponse<object>.Fail("Không thể xác nhận đơn đã hủy."));

        d.TrangThai = "Confirmed";
        d.NgayCapNhat = DateTime.UtcNow;

        if (daThuCoc && d.TienDatCoc > 0)
        {
            var hd = await db.HoaDons.Include(h => h.ThanhToans).FirstOrDefaultAsync(h => h.MaDatPhong == d.MaId);
            if (hd != null && !hd.ThanhToans.Any(t => t.MaGiaoDich != null && t.MaGiaoDich.StartsWith("COC-")))
            {
                db.ThanhToans.Add(new ThanhToan
                {
                    MaHoaDon = hd.MaId,
                    PhuongThucThanhToan = "BankTransfer",
                    SoTienDaThanhToan = d.TienDatCoc,
                    MaGiaoDich = $"COC-{d.MaDatPhong}",
                    TrangThai = "Success",
                    NgayThanhToan = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "XAC_NHAN_DAT_PHONG",
            doiTuong: "DatPhong",
            khoaChinh: d.MaDatPhong,
            noiDung: $"Xác nhận đơn đặt phòng #{d.MaId} ({d.MaDatPhong}). Đã thu cọc: {daThuCoc}",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đã xác nhận đơn đặt phòng thành công."));
    }

    [Authorize]
    [HttpPut("{id:int}/huy")]
    public async Task<ActionResult<ApiResponse<object>>> HuyDatPhong(int id, [FromBody] HuyDatPhongRequest req)
    {
        var d = await db.DatPhongs
            .Include(x => x.ChiTietDatPhongs).ThenInclude(ct => ct.MaPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaId == id);
        if (d == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đặt phòng."));

        var currentUserId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && d.MaNguoiDung != currentUserId)
            return Forbid();

        if (StatusMapper.IsBookingCancelled(d.TrangThai))
            return BadRequest(ApiResponse<object>.Fail("Đặt phòng đã bị hủy trước đó."));

        if (!User.IsStaffOrAdmin() && (StatusMapper.IsBookingCheckedIn(d.TrangThai) || StatusMapper.IsBookingCheckedOut(d.TrangThai)))
            return BadRequest(ApiResponse<object>.Fail("Không thể hủy đơn đặt phòng đang lưu trú hoặc đã hoàn thành."));

        d.TrangThai = "Cancelled";
        d.LyDoHuy = req.LyDoHuy;
        d.NgayCapNhat = DateTime.UtcNow;
        foreach (var ct in d.ChiTietDatPhongs)
        {
            ct.TrangThai = "Cancelled";
            if (ct.MaPhongNavigation != null && (ct.MaPhongNavigation.TrangThai == "Reserved" || ct.MaPhongNavigation.TrangThai == "Occupied"))
            {
                ct.MaPhongNavigation.TrangThai = "Available";
            }
        }

        var hd = await db.HoaDons.FirstOrDefaultAsync(h => h.MaDatPhong == d.MaId);
        if (hd != null && StatusMapper.IsInvoicePending(hd.TrangThai))
        {
            hd.TrangThai = "Cancelled";
        }

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "HUY_DAT_PHONG",
            doiTuong: "DatPhong",
            khoaChinh: d.MaDatPhong,
            noiDung: $"Đơn đặt phòng #{d.MaId} bị hủy. Lý do: {req.LyDoHuy}",
            maNguoiDung: currentUserId,
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Hủy đặt phòng thành công."));
    }

    [Authorize]
    [HttpPut("chi-tiet/{chiTietId:int}/gan-phong")]
    public async Task<ActionResult<ApiResponse<object>>> GanPhong(int chiTietId, [FromBody] GanPhongRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var ct = await db.ChiTietDatPhongs
            .Include(x => x.MaDatPhongNavigation)
            .Include(x => x.MaPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaId == chiTietId);

        if (ct == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy chi tiết đặt phòng."));

        var phong = await db.Phongs.FindAsync(req.MaPhong);
        if (phong == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy phòng được chỉ định."));

        if (phong.MaLoaiPhong != ct.MaLoaiPhong)
            return BadRequest(ApiResponse<object>.Fail("Phòng được chọn không thuộc loại phòng của đơn đặt này."));

        if (StatusMapper.IsRoomMaintenance(phong.TrangThai))
            return BadRequest(ApiResponse<object>.Fail($"Phòng '{phong.SoPhong}' đang trong trạng thái bảo dưỡng."));

        var daCoNguoiDat = await db.ChiTietDatPhongs.AnyAsync(other =>
            other.MaId != ct.MaId
            && other.MaPhong == req.MaPhong
            && other.TrangThai != "Cancelled" && other.TrangThai != "da_huy"
            && other.TrangThai != "CheckedOut" && other.TrangThai != "hoan_thanh"
            && other.NgayNhanPhongDuKien < ct.NgayTraPhongDuKien
            && other.NgayTraPhongDuKien > ct.NgayNhanPhongDuKien);

        if (daCoNguoiDat)
            return BadRequest(ApiResponse<object>.Fail($"Phòng '{phong.SoPhong}' đã có khách đặt trong khoảng thời gian này."));

        ct.MaPhong = req.MaPhong;
        if (ct.TrangThai == "Active" || ct.TrangThai == "dang_o")
            phong.TrangThai = "Occupied";
        else if (phong.TrangThai == "Available")
            phong.TrangThai = "Reserved";

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "GAN_PHONG",
            doiTuong: "ChiTietDatPhong",
            khoaChinh: ct.MaId.ToString(),
            noiDung: $"Gán phòng '{phong.SoPhong}' cho chi tiết đặt phòng #{ct.MaId} (Đơn: {ct.MaDatPhongNavigation?.MaDatPhong})",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, $"Gán phòng '{phong.SoPhong}' thành công."));
    }

    [Authorize]
    [HttpPut("{id:int}/check-in")]
    public async Task<ActionResult<ApiResponse<object>>> CheckIn(int id)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var d = await db.DatPhongs
            .Include(x => x.ChiTietDatPhongs).ThenInclude(ct => ct.MaPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaId == id);
        if (d == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đặt phòng."));

        if (StatusMapper.IsBookingCancelled(d.TrangThai))
            return BadRequest(ApiResponse<object>.Fail("Đơn đặt phòng này đã bị hủy."));

        if (StatusMapper.IsBookingCheckedIn(d.TrangThai))
            return BadRequest(ApiResponse<object>.Fail("Đơn đặt phòng này đã được check-in trước đó."));

        d.TrangThai = "CheckedIn";
        d.NgayCapNhat = DateTime.UtcNow;

        foreach (var ct in d.ChiTietDatPhongs.Where(ct => ct.TrangThai == "Pending" || ct.TrangThai == "xac_nhan" || ct.TrangThai == "cho_xac_nhan"))
        {
            ct.ThoiGianNhanPhongThucTe = DateTime.UtcNow;
            ct.TrangThai = "Active";

            if (ct.MaPhong == null || ct.MaPhongNavigation == null)
            {
                var phongTrong = await db.Phongs.FirstOrDefaultAsync(p =>
                    p.MaLoaiPhong == ct.MaLoaiPhong
                    && (p.TrangThai == "Available" || p.TrangThai == "trong")
                    && (p.TrangThaiVeSinh == "Clean" || p.TrangThaiVeSinh == "sach_se"));

                if (phongTrong != null)
                {
                    ct.MaPhong = phongTrong.MaId;
                    ct.MaPhongNavigation = phongTrong;
                }
            }

            if (ct.MaPhongNavigation != null)
            {
                ct.MaPhongNavigation.TrangThai = "Occupied";
            }
        }
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CHECK_IN",
            doiTuong: "DatPhong",
            khoaChinh: d.MaDatPhong,
            noiDung: $"Check-in thành công cho đơn đặt phòng #{d.MaId}",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Check-in thành công."));
    }

    [Authorize]
    [HttpPut("{id:int}/check-out")]
    public async Task<ActionResult<ApiResponse<object>>> CheckOut(int id)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var d = await db.DatPhongs
            .Include(x => x.ChiTietDatPhongs).ThenInclude(ct => ct.MaPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaId == id);
        if (d == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đặt phòng."));

        d.TrangThai = "CheckedOut";
        d.NgayCapNhat = DateTime.UtcNow;

        foreach (var ct in d.ChiTietDatPhongs.Where(ct => ct.TrangThai == "Active" || ct.TrangThai == "dang_o"))
        {
            ct.ThoiGianTraPhongThucTe = DateTime.UtcNow;
            ct.TrangThai = "CheckedOut";

            if (ct.MaPhongNavigation != null)
            {
                // Chuyển về Available nhưng Dirty để khóa xếp khách cho tới khi hoàn tất vệ sinh
                ct.MaPhongNavigation.TrangThai = "Available";
                ct.MaPhongNavigation.TrangThaiVeSinh = "Dirty";

                db.NhatKyBuongPhongs.Add(new NhatKyBuongPhong
                {
                    MaPhong = ct.MaPhongNavigation.MaId,
                    MaChiTietDatPhong = ct.MaId,
                    TrangThai = "assigned",
                    ThoiGianPhanCong = DateTime.UtcNow,
                    GhiChuKiemTra = $"Khách trả phòng đơn #{d.MaDatPhong}. Cần dọn dẹp vệ sinh và kiểm tra tài sản.",
                    NgayTao = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CHECK_OUT",
            doiTuong: "DatPhong",
            khoaChinh: d.MaDatPhong,
            noiDung: $"Check-out thành công cho đơn đặt phòng #{d.MaId}. Phòng đã chuyển sang trạng thái chờ dọn dẹp (Dirty).",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Check-out thành công. Phòng đã được chuyển sang chế độ chờ dọn dẹp."));
    }

    private static DatPhongDto MapDatPhong(DatPhong d) => new()
    {
        MaId = d.MaId, MaDatPhong = d.MaDatPhong,
        TenKhach = d.TenKhach, SdtKhach = d.Sdtkhach, EmailKhach = d.EmailKhach,
        KenhDatPhong = d.KenhDatPhong, TrangThai = d.TrangThai,
        TongTien = d.TongTien, TienDatCoc = d.TienDatCoc,
        GhiChu = d.GhiChu, NgayTao = d.NgayTao,
        DanhSachPhong = d.ChiTietDatPhongs.Select(ct => new ChiTietDatPhongDto
        {
            MaId = ct.MaId, MaPhong = ct.MaPhong,
            SoPhong = ct.MaPhongNavigation?.SoPhong,
            MaLoaiPhong = ct.MaLoaiPhong,
            TenLoaiPhong = ct.MaLoaiPhongNavigation?.Ten ?? "",
            NgayNhanPhongDuKien = ct.NgayNhanPhongDuKien,
            NgayTraPhongDuKien = ct.NgayTraPhongDuKien,
            GiaMoiDem = ct.GiaMoiDem, TrangThai = ct.TrangThai,
            ThoiGianNhanPhongThucTe = ct.ThoiGianNhanPhongThucTe,
            ThoiGianTraPhongThucTe = ct.ThoiGianTraPhongThucTe
        }).ToList()
    };
}
