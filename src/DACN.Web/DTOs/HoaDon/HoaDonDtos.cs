using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.HoaDon;

public class HoaDonDto
{
    public int MaId { get; set; }
    public int MaDatPhong { get; set; }
    public string MaDatPhongCode { get; set; } = string.Empty;
    public string? TenKhach { get; set; }
    public decimal TongTienPhong { get; set; }
    public decimal TongTienDichVu { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TienThue { get; set; }
    public decimal TienPhuThu { get; set; }
    public string? LyDoPhuThu { get; set; }
    public decimal TongThanhToan { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
    public List<ThanhToanDto> DanhSachThanhToan { get; set; } = [];
}

public class ThanhToanDto
{
    public int MaId { get; set; }
    public string PhuongThucThanhToan { get; set; } = string.Empty;
    public decimal SoTienDaThanhToan { get; set; }
    public string? MaGiaoDich { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayThanhToan { get; set; }
}

public class TaoThanhToanRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã hóa đơn không hợp lệ.")]
    public int MaHoaDon { get; set; }

    [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
    [MaxLength(50, ErrorMessage = "Phương thức thanh toán tối đa 50 ký tự.")]
    public string PhuongThucThanhToan { get; set; } = string.Empty;

    [Range(1, 10000000000, ErrorMessage = "Số tiền thanh toán phải từ 1 đến 10,000,000,000 VNĐ.")]
    public decimal SoTienDaThanhToan { get; set; }

    [MaxLength(100, ErrorMessage = "Mã giao dịch tối đa 100 ký tự.")]
    public string? MaGiaoDich { get; set; }
}

public class CapNhatPhuThuRequest
{
    [Range(0, 10000000000, ErrorMessage = "Tiền phụ thu không hợp lệ.")]
    public decimal TienPhuThu { get; set; }

    [MaxLength(500)]
    public string? LyDoPhuThu { get; set; }
}

public class WebhookThanhToanRequest
{
    [Required]
    public int MaHoaDon { get; set; }
    [Required]
    public decimal SoTien { get; set; }
    [Required]
    public string MaGiaoDich { get; set; } = string.Empty;
    public string PhuongThuc { get; set; } = "VNPay";
    [Required]
    public string ChuKy { get; set; } = string.Empty;
}
