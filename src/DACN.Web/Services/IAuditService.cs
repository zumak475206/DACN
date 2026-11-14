using DACN.Web.DTOs.Audit;

namespace DACN.Web.Services;

public interface IAuditService
{
    Task GhiNhanAsync(
        string hanhDong,
        string doiTuong,
        string? khoaChinh = null,
        string? noiDung = null,
        int? maNguoiDung = null,
        string? email = null,
        string? diaChiIp = null,
        bool thanhCong = true);

    Task<(List<NhatKyHeThongDto> DanhSach, int TongSo)> LayDanhSachAsync(NhatKyFilterRequest filter);
}
