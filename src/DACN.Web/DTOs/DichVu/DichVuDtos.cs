using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.DichVu;

public class DichVuDto
{
    public int MaId { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public decimal GiaDichVu { get; set; }
    public string? DonViTinh { get; set; }
    public string? MoTa { get; set; }
    public bool DangHoatDong { get; set; }
}

public class DatDichVuRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã chi tiết đặt phòng không hợp lệ.")]
    public int MaChiTietDatPhong { get; set; }

    [Required(ErrorMessage = "Danh sách dịch vụ không được để trống.")]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất một dịch vụ.")]
    public List<ChiTietDatDichVuRequest> DanhSachDichVu { get; set; } = [];
}

public class ChiTietDatDichVuRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã dịch vụ không hợp lệ.")]
    public int MaDichVu { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng dịch vụ phải từ 1 đến 100.")]
    public int SoLuong { get; set; } = 1;

    [MaxLength(500, ErrorMessage = "Ghi chú dịch vụ tối đa 500 ký tự.")]
    public string? GhiChu { get; set; }
}

public class DonDichVuDto
{
    public int MaId { get; set; }
    public int MaChiTietDatPhong { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public decimal TongTien { get; set; }
    public DateTime NgayTao { get; set; }
    public List<ChiTietDonDichVuDto> ChiTiet { get; set; } = [];
}

public class ChiTietDonDichVuDto
{
    public int MaDichVu { get; set; }
    public string TenDichVu { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public string? GhiChu { get; set; }
}
