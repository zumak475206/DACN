namespace DACN.Web.DTOs.Audit;

public class NhatKyHeThongDto
{
    public int MaId { get; set; }
    public int? MaNguoiDung { get; set; }
    public string? Email { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string DoiTuong { get; set; } = string.Empty;
    public string? KhoaChinh { get; set; }
    public string? NoiDung { get; set; }
    public string? DiaChiIp { get; set; }
    public bool ThanhCong { get; set; }
    public DateTime ThoiGian { get; set; }
}

public class NhatKyFilterRequest
{
    public int Trang { get; set; } = 1;
    public int SoLuong { get; set; } = 20;
    public string? HanhDong { get; set; }
    public string? DoiTuong { get; set; }
    public string? Email { get; set; }
}
