using DACN.Infrastructure.Data;
using DACN.Web.DTOs.Audit;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Services;

public class AuditService : IAuditService
{
    private readonly DACNContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(DACNContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task GhiNhanAsync(
        string hanhDong,
        string doiTuong,
        string? khoaChinh = null,
        string? noiDung = null,
        int? maNguoiDung = null,
        string? email = null,
        string? diaChiIp = null,
        bool thanhCong = true)
    {
        // 1. Luôn ghi log có cấu trúc vào ILogger (đảm bảo an toàn không phụ thuộc DB)
        _logger.LogInformation(
            "AUDIT [{HanhDong}] trên [{DoiTuong}] (Khoá: {KhoaChinh}) bởi User: {Email} (ID: {MaNguoiDung}) từ IP: {Ip} - Trạng thái: {TrangThai}. Chi tiết: {ChiTiet}",
            hanhDong, doiTuong, khoaChinh ?? "N/A", email ?? "Hệ thống/Ẩn danh", maNguoiDung?.ToString() ?? "N/A",
            diaChiIp ?? "N/A", thanhCong ? "Thành công" : "Thất bại", noiDung ?? "");

        // 2. Lưu trực tiếp vào Database qua EF Core Entity
        try
        {
            var log = new NhatKyHeThong
            {
                MaNguoiDung = maNguoiDung,
                Email = email,
                HanhDong = hanhDong,
                DoiTuong = doiTuong,
                KhoaChinh = khoaChinh,
                NoiDung = noiDung,
                DiaChiIp = diaChiIp,
                ThanhCong = thanhCong,
                ThoiGian = DateTime.UtcNow
            };

            _db.NhatKyHeThongs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu nhật ký kiểm toán vào Database: {Message}", ex.Message);
        }
    }

    public async Task<(List<NhatKyHeThongDto> DanhSach, int TongSo)> LayDanhSachAsync(NhatKyFilterRequest filter)
    {
        try
        {
            var query = _db.NhatKyHeThongs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.HanhDong))
                query = query.Where(x => x.HanhDong == filter.HanhDong);

            if (!string.IsNullOrWhiteSpace(filter.DoiTuong))
                query = query.Where(x => x.DoiTuong == filter.DoiTuong);

            if (!string.IsNullOrWhiteSpace(filter.Email))
                query = query.Where(x => x.Email != null && x.Email.Contains(filter.Email));

            var total = await query.CountAsync();

            var limit = Math.Clamp(filter.SoLuong, 1, 100);
            var offset = Math.Max(0, (filter.Trang - 1) * limit);

            var list = await query
                .OrderByDescending(x => x.ThoiGian)
                .Skip(offset)
                .Take(limit)
                .Select(x => new NhatKyHeThongDto
                {
                    MaId = x.MaId,
                    MaNguoiDung = x.MaNguoiDung,
                    Email = x.Email,
                    HanhDong = x.HanhDong,
                    DoiTuong = x.DoiTuong,
                    KhoaChinh = x.KhoaChinh,
                    NoiDung = x.NoiDung,
                    DiaChiIp = x.DiaChiIp,
                    ThanhCong = x.ThanhCong,
                    ThoiGian = x.ThoiGian
                })
                .ToListAsync();

            return (list, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi truy vấn danh sách nhật ký hệ thống: {Message}", ex.Message);
            return (new List<NhatKyHeThongDto>(), 0);
        }
    }
}
