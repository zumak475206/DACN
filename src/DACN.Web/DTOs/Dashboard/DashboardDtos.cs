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

public class HotelBiKpiDto
{
    public decimal CongSuatPhongPhanTram { get; set; }
    public decimal AdrGiaTrungBinhDem { get; set; }
    public decimal RevParDoanhThuTrenPhong { get; set; }
    public decimal TyLeHuyPhongPhanTram { get; set; }
    public decimal TongDoanhThuKy { get; set; }
    public decimal TongChiPhiVanHanhKy { get; set; }
    public decimal LoiNhuanRongKy { get; set; }
    public decimal DiemDanhGiaTrungBinh { get; set; }
    public List<DoanhThuTheoKenhDto> TheoKenhDat { get; set; } = [];
    public List<DoanhThuTheoLoaiPhongDto> TheoLoaiPhong { get; set; } = [];
}

public class DoanhThuTheoKenhDto
{
    public string Kenh { get; set; } = string.Empty;
    public int SoDon { get; set; }
    public decimal DoanhThu { get; set; }
}

public class DoanhThuTheoLoaiPhongDto
{
    public string TenLoaiPhong { get; set; } = string.Empty;
    public int SoDemBan { get; set; }
    public decimal DoanhThu { get; set; }
}
