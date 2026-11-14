using System.ComponentModel.DataAnnotations;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Controllers;

public class ChatAiRequest
{
    public string? MaPhienChat { get; set; }
    [Required]
    [MaxLength(1000)]
    public string CauHoi { get; set; } = string.Empty;
}

[Route("api/v1/ai")]
[ApiController]
public class AiController(DACNContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<object>>> Chat([FromBody] ChatAiRequest req)
    {
        var token = string.IsNullOrWhiteSpace(req.MaPhienChat)
            ? $"CHAT-{Guid.NewGuid():N}"[..20]
            : req.MaPhienChat.Trim();

        var session = await db.PhienChatAis.FirstOrDefaultAsync(s => s.MaPhienChat == token);
        if (session == null)
        {
            session = new PhienChatAi
            {
                MaPhienChat = token,
                MaNguoiDung = User.GetUserId(),
                TrangThai = "active",
                ThoiGianBatDau = DateTime.UtcNow
            };
            db.PhienChatAis.Add(session);
            await db.SaveChangesAsync();
        }

        db.TinNhanChatAis.Add(new TinNhanChatAi
        {
            MaPhienChat = session.MaId,
            NguoiGui = "user",
            NoiDungTinNhan = req.CauHoi,
            LoaiTinNhan = "text",
            NgayTao = DateTime.UtcNow
        });

        var q = req.CauHoi.ToLowerInvariant();
        string reply;
        if (q.Contains("giá") || q.Contains("phòng") || q.Contains("trống"))
        {
            var loaiPhongs = await db.LoaiPhongs
                .AsNoTracking()
                .Where(lp => lp.DangHoatDong)
                .Select(lp => $"{lp.Ten} ({lp.GiaCoBan:N0}đ/đêm)")
                .ToListAsync();
            reply = loaiPhongs.Any()
                ? $"Hiện tại Grand Hotel có các hạng phòng đang mở bán: {string.Join(", ", loaiPhongs)}. Quý khách có thể áp dụng mã giảm giá khi đặt trực tuyến!"
                : "Hiện tại khách sạn có các hạng phòng từ Standard đến Suite hướng biển với giá từ 550.000đ/đêm.";
        }
        else if (q.Contains("voucher") || q.Contains("giảm giá") || q.Contains("khuyến mãi"))
        {
            var vouchers = await db.MaGiamGia
                .AsNoTracking()
                .Where(v => v.DangHoatDong && v.SoLanDaDung < v.GioiHanSuDung)
                .Select(v => v.MaCode)
                .ToListAsync();
            reply = vouchers.Any()
                ? $"Các mã ưu đãi đang còn lượt áp dụng: {string.Join(", ", vouchers)}."
                : "Quý khách có thể kiểm tra mục Ưu Đãi Voucher trên trang chủ để nhận mã giảm giá mới nhất.";
        }
        else
        {
            var diaDiems = await db.DiaDiemThamQuans
                .AsNoTracking()
                .Where(d => d.DangHoatDong)
                .Take(3)
                .Select(d => $"{d.Ten} (cách {d.KhoangCachKm ?? 2m}km)")
                .ToListAsync();
            var goiY = diaDiems.Any() ? string.Join(", ", diaDiems) : "Biển Mỹ Khê, Cầu Rồng, Bán đảo Sơn Trà";
            reply = $"Trợ lý AI Grand Hotel gợi ý lịch trình tham quan gần khách sạn: {goiY}. Quý khách cần hỗ trợ đặt phòng hay dịch vụ đưa đón sân bay không ạ?";
        }

        db.TinNhanChatAis.Add(new TinNhanChatAi
        {
            MaPhienChat = session.MaId,
            NguoiGui = "ai",
            NoiDungTinNhan = reply,
            LoaiTinNhan = "text",
            NgayTao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            MaPhienChat = session.MaPhienChat,
            CauTraLoi = reply,
            ThoiGian = DateTime.UtcNow
        }));
    }

    [Authorize]
    [HttpGet("du-bao-nhu-cau")]
    public async Task<ActionResult<ApiResponse<object>>> DuBaoNhuCau()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var existing = await db.DuBaoNhuCauDatPhongAis
            .AsNoTracking()
            .Include(d => d.MaLoaiPhongNavigation)
            .OrderByDescending(d => d.NgayDuBao)
            .Take(14)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(existing));
    }

    [Authorize]
    [HttpGet("du-doan-huy-phong")]
    public async Task<ActionResult<ApiResponse<object>>> DuDoanHuyPhong()
    {
        if (!User.IsStaffOrAdmin())
            return Forbid();

        var list = await db.DuDoanHuyPhongAis
            .AsNoTracking()
            .Include(d => d.MaDatPhongNavigation)
            .OrderByDescending(d => d.XacSuatHuyPhong)
            .Take(20)
            .Select(d => new
            {
                d.MaId,
                d.MaDatPhong,
                MaDatPhongCode = d.MaDatPhongNavigation.MaDatPhong,
                TenKhach = d.MaDatPhongNavigation.TenKhach,
                d.XacSuatHuyPhong,
                d.MucDoRuiRo,
                d.YeuToChinh,
                d.ThoiGianDuDoan
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(list));
    }
}
