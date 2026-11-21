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

public class TaoDiaDiemRequest
{
    [Required(ErrorMessage = "Tên địa điểm không được để trống.")]
    [MaxLength(255)]
    public string Ten { get; set; } = string.Empty;
    public decimal? KhoangCachKm { get; set; }
    public string? MoTa { get; set; }
    public string? DiaChi { get; set; }
    public string? DuongDanAnh { get; set; }
    public string? LienKetBanDo { get; set; }
    public decimal? ViDo { get; set; }
    public decimal? KinhDo { get; set; }
    [MaxLength(200)]
    public string? PhongCach { get; set; }
}

public class TaoBaiVietRequest
{
    [Required(ErrorMessage = "Tiêu đề bài viết không được để trống.")]
    [MaxLength(255)]
    public string TieuDe { get; set; } = string.Empty;
    public string? DanhMuc { get; set; } = "TravelGuide";
    public string? NoiDung { get; set; }
    public string? DuongDanAnhBia { get; set; }
}

public class TaoThongBaoRequest
{
    public int? MaNguoiDung { get; set; }
    [Required]
    [MaxLength(255)]
    public string TieuDe { get; set; } = string.Empty;
    [Required]
    public string NoiDung { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? LoaiThongBao { get; set; } = "System";
}

[Authorize]
[Route("api/v1/noi-dung")]
[ApiController]
public class NoiDungController(DACNContext db, IAuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("dia-diem")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachDiaDiem([FromQuery] string? phongCach = null)
    {
        var query = db.DiaDiemThamQuans.AsNoTracking().Where(d => d.DangHoatDong);
        if (!string.IsNullOrWhiteSpace(phongCach))
        {
            var tag = phongCach.Trim().ToLowerInvariant();
            query = query.Where(d => d.PhongCach != null && d.PhongCach.ToLower().Contains(tag));
        }

        var list = await query
            .OrderBy(d => d.KhoangCachKm)
            .Select(d => new
            {
                d.MaId,
                d.Ten,
                d.KhoangCachKm,
                d.MoTa,
                d.DiaChi,
                d.DuongDanAnh,
                d.LienKetBanDo,
                d.ViDo,
                d.KinhDo,
                d.PhongCach
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(list));
    }

    [Authorize]
    [HttpPost("dia-diem")]
    public async Task<ActionResult<ApiResponse<object>>> TaoDiaDiem([FromBody] TaoDiaDiemRequest req)
    {
        if (!User.HasPermission(Quyen.ManageAIData) && !User.HasPermission(Quyen.ManageArticles))
            return Forbid();

        var item = new DiaDiemThamQuan
        {
            Ten = req.Ten.Trim(),
            KhoangCachKm = req.KhoangCachKm ?? 2.5m,
            MoTa = req.MoTa,
            DiaChi = req.DiaChi ?? "Đà Nẵng",
            DuongDanAnh = req.DuongDanAnh,
            LienKetBanDo = req.LienKetBanDo,
            ViDo = req.ViDo,
            KinhDo = req.KinhDo,
            PhongCach = req.PhongCach ?? "songao,thugian",
            DangHoatDong = true
        };
        db.DiaDiemThamQuans.Add(item);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "TAO_DIA_DIEM",
            doiTuong: "DiaDiemThamQuan",
            khoaChinh: item.MaId.ToString(),
            noiDung: $"Thêm địa điểm tham quan '{item.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(new { item.MaId, item.Ten }, "Thêm địa điểm tham quan thành công."));
    }

    [Authorize]
    [HttpPut("dia-diem/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatDiaDiem(int id, [FromBody] TaoDiaDiemRequest req)
    {
        if (!User.HasPermission(Quyen.ManageAIData) && !User.HasPermission(Quyen.ManageArticles))
            return Forbid();

        var item = await db.DiaDiemThamQuans.FindAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy địa điểm."));

        item.Ten = req.Ten.Trim();
        item.KhoangCachKm = req.KhoangCachKm ?? item.KhoangCachKm;
        item.MoTa = req.MoTa;
        item.DiaChi = req.DiaChi ?? item.DiaChi;
        item.DuongDanAnh = req.DuongDanAnh ?? item.DuongDanAnh;
        item.LienKetBanDo = req.LienKetBanDo ?? item.LienKetBanDo;
        item.ViDo = req.ViDo ?? item.ViDo;
        item.KinhDo = req.KinhDo ?? item.KinhDo;
        item.PhongCach = req.PhongCach ?? item.PhongCach;

        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_DIA_DIEM",
            doiTuong: "DiaDiemThamQuan",
            khoaChinh: item.MaId.ToString(),
            noiDung: $"Cập nhật địa điểm '{item.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật địa điểm thành công."));
    }

    [Authorize]
    [HttpDelete("dia-diem/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> XoaDiaDiem(int id)
    {
        if (!User.HasPermission(Quyen.ManageAIData) && !User.HasPermission(Quyen.ManageArticles))
            return Forbid();

        var item = await db.DiaDiemThamQuans.FindAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy địa điểm."));

        item.DangHoatDong = false;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "XOA_DIA_DIEM",
            doiTuong: "DiaDiemThamQuan",
            khoaChinh: item.MaId.ToString(),
            noiDung: $"Ẩn địa điểm '{item.Ten}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đã ẩn địa điểm tham quan."));
    }

    [AllowAnonymous]
    [HttpGet("bai-viet")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachBaiViet([FromQuery] string? danhMuc = null)
    {
        var query = db.BaiViets
            .AsNoTracking()
            .Include(b => b.MaTacGiaNavigation)
            .Where(b => b.DangHoatDong);

        if (!string.IsNullOrWhiteSpace(danhMuc))
            query = query.Where(b => b.DanhMuc == danhMuc);

        var list = await query
            .OrderByDescending(b => b.NgayDang ?? b.NgayTao)
            .Select(b => new
            {
                b.MaId,
                b.TieuDe,
                b.DuongDanRutGon,
                b.DanhMuc,
                b.NoiDung,
                b.DuongDanAnhBia,
                TacGia = b.MaTacGiaNavigation != null ? b.MaTacGiaNavigation.HoTen : "Ban Biên Tập",
                NgayDang = b.NgayDang ?? b.NgayTao
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(list));
    }

    [AllowAnonymous]
    [HttpGet("bai-viet/{slug}")]
    public async Task<ActionResult<ApiResponse<object>>> ChiTietBaiViet(string slug)
    {
        var bv = await db.BaiViets
            .AsNoTracking()
            .Include(b => b.MaTacGiaNavigation)
            .FirstOrDefaultAsync(b => b.DangHoatDong && b.DuongDanRutGon == slug);

        if (bv == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy bài viết."));

        return Ok(ApiResponse<object>.Ok(new
        {
            bv.MaId,
            bv.TieuDe,
            bv.DuongDanRutGon,
            bv.DanhMuc,
            bv.NoiDung,
            bv.DuongDanAnhBia,
            TacGia = bv.MaTacGiaNavigation?.HoTen ?? "Ban Biên Tập",
            NgayDang = bv.NgayDang ?? bv.NgayTao
        }));
    }

    [Authorize]
    [HttpPost("bai-viet")]
    public async Task<ActionResult<ApiResponse<object>>> TaoBaiViet([FromBody] TaoBaiVietRequest req)
    {
        if (!User.HasPermission(Quyen.ManageArticles))
            return Forbid();

        var baseSlug = GioVN.TaoSlug(req.TieuDe);
        var slug = baseSlug;
        if (await db.BaiViets.AnyAsync(b => b.DuongDanRutGon == slug))
            slug = $"{baseSlug}-{DateTime.UtcNow:MMddHHmmss}";

        var bv = new BaiViet
        {
            MaTacGia = User.GetUserId(),
            DanhMuc = req.DanhMuc ?? "TravelGuide",
            TieuDe = req.TieuDe.Trim(),
            DuongDanRutGon = slug,
            NoiDung = req.NoiDung,
            DuongDanAnhBia = req.DuongDanAnhBia,
            NgayDang = DateTime.UtcNow,
            DangHoatDong = true,
            NgayTao = DateTime.UtcNow
        };
        db.BaiViets.Add(bv);
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "TAO_BAI_VIET",
            doiTuong: "BaiViet",
            khoaChinh: bv.MaId.ToString(),
            noiDung: $"Đăng bài viết '{bv.TieuDe}' (slug: {bv.DuongDanRutGon})",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(new { bv.MaId, bv.TieuDe, bv.DuongDanRutGon }, "Đăng bài viết thành công."));
    }

    [Authorize]
    [HttpPut("bai-viet/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CapNhatBaiViet(int id, [FromBody] TaoBaiVietRequest req)
    {
        if (!User.HasPermission(Quyen.ManageArticles))
            return Forbid();

        var bv = await db.BaiViets.FindAsync(id);
        if (bv == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy bài viết."));

        bv.TieuDe = req.TieuDe.Trim();
        bv.DanhMuc = req.DanhMuc ?? bv.DanhMuc;
        bv.NoiDung = req.NoiDung;
        bv.DuongDanAnhBia = req.DuongDanAnhBia ?? bv.DuongDanAnhBia;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "CAP_NHAT_BAI_VIET",
            doiTuong: "BaiViet",
            khoaChinh: bv.MaId.ToString(),
            noiDung: $"Cập nhật bài viết '{bv.TieuDe}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Cập nhật bài viết thành công."));
    }

    [Authorize]
    [HttpDelete("bai-viet/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> XoaBaiViet(int id)
    {
        if (!User.HasPermission(Quyen.ManageArticles))
            return Forbid();

        var bv = await db.BaiViets.FindAsync(id);
        if (bv == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy bài viết."));

        bv.DangHoatDong = false;
        await db.SaveChangesAsync();

        await auditService.GhiNhanAsync(
            hanhDong: "XOA_BAI_VIET",
            doiTuong: "BaiViet",
            khoaChinh: bv.MaId.ToString(),
            noiDung: $"Ẩn bài viết '{bv.TieuDe}'",
            maNguoiDung: User.GetUserId(),
            email: User.FindFirst(ClaimTypes.Email)?.Value,
            diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            thanhCong: true);

        return Ok(ApiResponse<object>.Ok(null!, "Đã ẩn bài viết."));
    }

    [Authorize]
    [HttpGet("thong-bao")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachThongBao()
    {
        var userId = User.GetUserId();
        var query = db.ThongBaos.AsNoTracking().AsQueryable();
        if (userId.HasValue && !User.IsAdmin())
            query = query.Where(t => t.MaNguoiDung == userId.Value);

        var list = await query
            .OrderByDescending(t => t.NgayTao)
            .Take(30)
            .Select(t => new
            {
                t.MaId,
                t.MaNguoiDung,
                t.TieuDe,
                t.NoiDung,
                t.LoaiThongBao,
                t.DaDoc,
                t.NgayTao
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(list));
    }

    [Authorize]
    [HttpPost("thong-bao")]
    public async Task<ActionResult<ApiResponse<object>>> GuiThongBao([FromBody] TaoThongBaoRequest req)
    {
        if (!User.IsManagerOrAdmin() && !User.HasPermission(Quyen.ManageUsers))
            return Forbid();

        var tb = new ThongBao
        {
            MaNguoiDung = req.MaNguoiDung ?? User.GetUserId() ?? 1,
            TieuDe = req.TieuDe.Trim(),
            NoiDung = req.NoiDung.Trim(),
            LoaiThongBao = req.LoaiThongBao ?? "System",
            DaDoc = false,
            NgayTao = DateTime.UtcNow
        };
        db.ThongBaos.Add(tb);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { tb.MaId }, "Gửi thông báo thành công."));
    }

    [Authorize]
    [HttpPut("thong-bao/{id:int}/da-doc")]
    public async Task<ActionResult<ApiResponse<object>>> DanhDauDaDoc(int id)
    {
        var tb = await db.ThongBaos.FindAsync(id);
        if (tb == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông báo."));

        var userId = User.GetUserId();
        if (!User.IsStaffOrAdmin() && tb.MaNguoiDung != userId)
            return Forbid();

        tb.DaDoc = true;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Đã đánh dấu đã đọc."));
    }
}
