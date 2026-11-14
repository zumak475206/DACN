using System.ComponentModel.DataAnnotations;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

public class TaoDiaDiemRequest
{
    [Required]
    [MaxLength(255)]
    public string Ten { get; set; } = string.Empty;
    public decimal? KhoangCachKm { get; set; }
    public string? MoTa { get; set; }
    public string? DiaChi { get; set; }
    public string? DuongDanAnh { get; set; }
}

public class TaoBaiVietRequest
{
    [Required]
    [MaxLength(255)]
    public string TieuDe { get; set; } = string.Empty;
    public string? DanhMuc { get; set; } = "TravelGuide";
    public string? NoiDung { get; set; }
    public string? DuongDanAnhBia { get; set; }
}

[Route("api/v1/noi-dung")]
[ApiController]
public class NoiDungController(DACNContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("dia-diem")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachDiaDiem()
    {
        var list = await db.DiaDiemThamQuans
            .AsNoTracking()
            .Where(d => d.DangHoatDong)
            .OrderBy(d => d.KhoangCachKm)
            .Select(d => new
            {
                d.MaId,
                d.Ten,
                d.KhoangCachKm,
                d.MoTa,
                d.DiaChi,
                d.DuongDanAnh,
                d.LienKetBanDo
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(list));
    }

    [Authorize]
    [HttpPost("dia-diem")]
    public async Task<ActionResult<ApiResponse<object>>> TaoDiaDiem([FromBody] TaoDiaDiemRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var item = new DiaDiemThamQuan
        {
            Ten = req.Ten,
            KhoangCachKm = req.KhoangCachKm ?? 2.5m,
            MoTa = req.MoTa,
            DiaChi = req.DiaChi ?? "Đà Nẵng",
            DuongDanAnh = req.DuongDanAnh,
            DangHoatDong = true
        };
        db.DiaDiemThamQuans.Add(item);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { item.MaId, item.Ten }, "Thêm địa điểm tham quan thành công."));
    }

    [AllowAnonymous]
    [HttpGet("bai-viet")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachBaiViet()
    {
        var list = await db.BaiViets
            .AsNoTracking()
            .Include(b => b.MaTacGiaNavigation)
            .Where(b => b.DangHoatDong)
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

    [Authorize]
    [HttpPost("bai-viet")]
    public async Task<ActionResult<ApiResponse<object>>> TaoBaiViet([FromBody] TaoBaiVietRequest req)
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var slug = req.TieuDe.ToLowerInvariant().Replace(" ", "-") + "-" + DateTime.UtcNow.ToString("MMddHHmm");
        var bv = new BaiViet
        {
            MaTacGia = User.GetUserId(),
            DanhMuc = req.DanhMuc ?? "TravelGuide",
            TieuDe = req.TieuDe,
            DuongDanRutGon = slug,
            NoiDung = req.NoiDung,
            DuongDanAnhBia = req.DuongDanAnhBia,
            NgayDang = DateTime.UtcNow,
            DangHoatDong = true,
            NgayTao = DateTime.UtcNow
        };
        db.BaiViets.Add(bv);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { bv.MaId, bv.TieuDe }, "Đăng bài viết thành công."));
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
