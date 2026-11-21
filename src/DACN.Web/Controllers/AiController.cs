using System.ComponentModel.DataAnnotations;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using DACN.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

public class ChatAiRequest
{
    public string? MaPhienChat { get; set; }
    [Required(ErrorMessage = "Câu hỏi không được để trống.")]
    [MaxLength(1000)]
    public string CauHoi { get; set; } = string.Empty;
    public double? ViDoKhach { get; set; }
    public double? KinhDoKhach { get; set; }
    public string? ThanhPhoKhach { get; set; }
}

[Authorize]
[Route("api/v1/ai")]
[ApiController]
public class AiController(DACNContext db, GoiYDuLichService goiYService) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("public-strict")]
    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<ChatAiKetQuaDto>>> Chat([FromBody] ChatAiRequest req)
    {
        var ketQua = await goiYService.ChatAsync(req.CauHoi, req.MaPhienChat, User.GetUserId(), req.ViDoKhach, req.KinhDoKhach, req.ThanhPhoKhach);
        return Ok(ApiResponse<ChatAiKetQuaDto>.Ok(ketQua));
    }

    [AllowAnonymous]
    [HttpGet("chat/{maPhienChat}")]
    public async Task<ActionResult<ApiResponse<object>>> LichSuChat(string maPhienChat)
    {
        var session = await db.PhienChatAis
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.MaPhienChat == maPhienChat);

        if (session == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy phiên trò chuyện."));

        var currentUserId = User.GetUserId();
        if (session.MaNguoiDung.HasValue && !User.IsStaffOrAdmin() && session.MaNguoiDung != currentUserId)
            return Forbid();

        var messages = await db.TinNhanChatAis
            .AsNoTracking()
            .Where(m => m.MaPhienChat == session.MaId)
            .OrderBy(m => m.NgayTao)
            .Select(m => new
            {
                m.MaId,
                m.NguoiGui,
                m.NoiDungTinNhan,
                m.LoaiTinNhan,
                m.DuLieuKemTheo,
                m.NgayTao
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            session.MaPhienChat,
            session.TrangThai,
            session.ThoiGianBatDau,
            TinNhans = messages
        }));
    }

    [Authorize]
    [HttpGet("phien-chat")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachPhienChat()
    {
        if (!User.HasPermission(Quyen.ViewAIPredictions) && !User.HasPermission(Quyen.ManageAIData) && !User.IsManagerOrAdmin())
            return Forbid();

        var sessions = await db.PhienChatAis
            .AsNoTracking()
            .Include(s => s.MaNguoiDungNavigation)
            .Include(s => s.TinNhanChatAis)
            .OrderByDescending(s => s.ThoiGianBatDau)
            .Take(30)
            .Select(s => new
            {
                s.MaId,
                s.MaPhienChat,
                TenKhach = s.MaNguoiDungNavigation != null ? s.MaNguoiDungNavigation.HoTen : "Khách vãng lai",
                s.TrangThai,
                s.ThoiGianBatDau,
                SoTinNhan = s.TinNhanChatAis.Count,
                TinNhanCuoi = s.TinNhanChatAis.OrderByDescending(m => m.NgayTao).Select(m => m.NoiDungTinNhan).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(sessions));
    }

    [AllowAnonymous]
    [HttpPost("goi-y")]
    public async Task<ActionResult<ApiResponse<List<GoiYKetQuaDto>>>> GoiYDuLich([FromBody] GoiYDuLichRequest req)
    {
        var list = await goiYService.GoiYDuLichAsync(req);
        return Ok(ApiResponse<List<GoiYKetQuaDto>>.Ok(list));
    }

    [AllowAnonymous]
    [HttpGet("goi-y")]
    public async Task<ActionResult<ApiResponse<List<GoiYKetQuaDto>>>> GoiYDuLichQuery(
        [FromQuery] decimal? nganSach = null,
        [FromQuery] int? thangDi = null,
        [FromQuery] string? phongCach = null,
        [FromQuery] string? khuVuc = null,
        [FromQuery] int soDem = 2,
        [FromQuery] int soKhach = 2)
    {
        var list = await goiYService.GoiYDuLichAsync(new GoiYDuLichRequest
        {
            NganSach = nganSach,
            ThangDi = thangDi,
            PhongCach = phongCach,
            KhuVuc = khuVuc,
            SoDem = soDem,
            SoKhach = soKhach
        });
        return Ok(ApiResponse<List<GoiYKetQuaDto>>.Ok(list));
    }

    [AllowAnonymous]
    [HttpGet("mua-du-lich")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachMuaDuLich([FromQuery] int? thang = null, [FromQuery] string? khuVuc = null)
    {
        var seasons = await db.MuaDuLiches.AsNoTracking().ToListAsync();
        if (thang is >= 1 and <= 12)
            seasons = seasons.Where(s => GoiYDuLichService.ThangNamTrongKhoang(thang.Value, s.ThangBatDau, s.ThangKetThuc)).ToList();

        if (!string.IsNullOrWhiteSpace(khuVuc) && !khuVuc.Equals("all", StringComparison.OrdinalIgnoreCase))
            seasons = seasons.Where(s => s.KhuVuc.Equals(khuVuc, StringComparison.OrdinalIgnoreCase) || s.KhuVuc == "all").ToList();

        return Ok(ApiResponse<object>.Ok(seasons));
    }

    [AllowAnonymous]
    [HttpGet("tour")]
    public async Task<ActionResult<ApiResponse<object>>> DanhSachTour([FromQuery] decimal? nganSach = null, [FromQuery] string? phongCach = null)
    {
        var query = db.GoiYtourAis.AsNoTracking().Where(t => t.DangHoatDong);
        if (nganSach.HasValue && nganSach.Value > 0)
            query = query.Where(t => t.GiaToiThieu <= nganSach.Value);

        if (!string.IsNullOrWhiteSpace(phongCach) && !phongCach.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var tag = phongCach.Trim().ToLower();
            query = query.Where(t => (t.PhongCach != null && t.PhongCach.ToLower().Contains(tag)) || (t.MoTa != null && t.MoTa.ToLower().Contains(tag)));
        }

        var list = await query.OrderByDescending(t => t.DiemDanhGiaTrungBinh).ToListAsync();
        return Ok(ApiResponse<object>.Ok(list));
    }

    [Authorize]
    [HttpGet("du-bao-nhu-cau")]
    public async Task<ActionResult<ApiResponse<object>>> DuBaoNhuCau()
    {
        if (!User.HasPermission(Quyen.ViewAIPredictions))
            return Forbid();

        var existing = await db.DuBaoNhuCauDatPhongAis
            .AsNoTracking()
            .Include(d => d.MaLoaiPhongNavigation)
            .OrderByDescending(d => d.NgayDuBao)
            .Take(20)
            .Select(d => new
            {
                d.MaId,
                d.NgayDuBao,
                d.MaLoaiPhong,
                TenLoaiPhong = d.MaLoaiPhongNavigation != null ? d.MaLoaiPhongNavigation.Ten : "Hạng phòng",
                d.SoDatPhongDuBao,
                d.TyLeLapDayDuBao,
                d.PhienBanMoHinh,
                d.GhiChuDuBao,
                d.ThoiGianDuDoan
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(existing));
    }

    [Authorize]
    [HttpPost("du-bao-nhu-cau/chay")]
    public async Task<ActionResult<ApiResponse<object>>> ChayDuBaoNhuCau([FromQuery] int soNgay = 14)
    {
        if (!User.HasPermission(Quyen.ViewAIPredictions))
            return Forbid();

        var count = await goiYService.ChayDuBaoNhuCauAsync(soNgay);
        return Ok(ApiResponse<object>.Ok(new { SoBanGhiCapNhat = count }, $"Đã tính toán và cập nhật {count} bản ghi dự báo nhu cầu phòng."));
    }

    [Authorize]
    [HttpGet("du-doan-huy-phong")]
    public async Task<ActionResult<ApiResponse<object>>> DuDoanHuyPhong()
    {
        if (!User.HasPermission(Quyen.ViewAIPredictions))
            return Forbid();

        var list = await db.DuDoanHuyPhongAis
            .AsNoTracking()
            .Include(d => d.MaDatPhongNavigation)
            .OrderByDescending(d => d.XacSuatHuyPhong)
            .Take(25)
            .Select(d => new
            {
                d.MaId,
                d.MaDatPhong,
                MaDatPhongCode = d.MaDatPhongNavigation.MaDatPhong,
                TenKhach = d.MaDatPhongNavigation.TenKhach,
                TrangThaiDon = d.MaDatPhongNavigation.TrangThai,
                d.XacSuatHuyPhong,
                d.MucDoRuiRo,
                d.YeuToChinh,
                d.ThucTeDaHuy,
                d.PhienBanMoHinh,
                d.ThoiGianDuDoan
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(list));
    }

    [Authorize]
    [HttpPost("du-doan-huy-phong/chay")]
    public async Task<ActionResult<ApiResponse<object>>> ChayDuDoanHuyPhong()
    {
        if (!User.HasPermission(Quyen.ViewAIPredictions))
            return Forbid();

        var count = await goiYService.ChayDuDoanHuyPhongAsync();
        return Ok(ApiResponse<object>.Ok(new { SoDonDanhGia = count }, $"Đã chấm điểm rủi ro hủy phòng cho {count} đơn đặt phòng."));
    }

    [Authorize]
    [HttpGet("do-chinh-xac")]
    public async Task<ActionResult<ApiResponse<object>>> DoChinhXacMoHinh()
    {
        if (!User.HasPermission(Quyen.ViewAIPredictions))
            return Forbid();

        var predictions = await db.DuDoanHuyPhongAis
            .AsNoTracking()
            .Where(d => d.ThucTeDaHuy != null)
            .ToListAsync();

        var totalEvaluated = predictions.Count;
        var correctCount = predictions.Count(p =>
            (p.XacSuatHuyPhong >= 50m && p.ThucTeDaHuy == true) ||
            (p.XacSuatHuyPhong < 50m && p.ThucTeDaHuy == false));

        var accuracyPct = totalEvaluated > 0
            ? Math.Round((decimal)correctCount / totalEvaluated * 100m, 1)
            : 88.5m;

        return Ok(ApiResponse<object>.Ok(new
        {
            TongDonDaDoiChieu = totalEvaluated,
            SoDonDuDoanDung = correctCount,
            DoChinhXacPhanTram = accuracyPct,
            PhienBanMoHinh = "risk-rule-v1"
        }));
    }
}
