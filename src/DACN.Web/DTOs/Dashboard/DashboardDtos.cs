using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.Dashboard;

public class DashboardAdminDto
{
    public int TongPhong { get; set; }
    public int PhongDangCoKhach { get; set; }
    public int PhongTrong { get; set; }
    public int PhongBaoDuong { get; set; }
    public decimal DoanhThuThang { get; set; }
    public decimal DoanhThuHomNay { get; set; }
    public int TongDatPhongHomNay { get; set; }
    public int DatPhongChoXacNhan { get; set; }
    public List<DatPhongMoiDto> DatPhongMoiNhat { get; set; } = [];
}

public class DatPhongMoiDto
{
    public int MaId { get; set; }
    public string MaDatPhong { get; set; } = string.Empty;
    public string? TenKhach { get; set; }
    public decimal TongTien { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
}

public class ThongKeDoanhThuRequest
{
    [Required(ErrorMessage = "Từ ngày không được để trống.")]
    public DateTime TuNgay { get; set; }

    [Required(ErrorMessage = "Đến ngày không được để trống.")]
    public DateTime DenNgay { get; set; }
}

public class ThongKeDoanhThuDto
{
    public DateTime Ngay { get; set; }
    public decimal DoanhThu { get; set; }
    public int SoDatPhong { get; set; }
}
