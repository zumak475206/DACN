using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.Voucher;

public class VoucherDto
{
    public int MaId { get; set; }
    public string MaCode { get; set; } = string.Empty;
    public string LoaiGiamGia { get; set; } = string.Empty;
    public decimal GiaTriGiamGia { get; set; }
    public decimal? GiaTriDatPhongToiThieu { get; set; }
    public decimal? GiamToiDa { get; set; }
    public DateTime HieuLucTu { get; set; }
    public DateTime HieuLucDen { get; set; }
    public int GioiHanSuDung { get; set; }
    public int SoLanDaDung { get; set; }
    public bool DangHoatDong { get; set; }
}

public class VoucherCongKhaiDto
{
    public string MaCode { get; set; } = string.Empty;
    public string LoaiGiamGia { get; set; } = string.Empty;
    public decimal GiaTriGiamGia { get; set; }
    public decimal? GiaTriDatPhongToiThieu { get; set; }
    public decimal? GiamToiDa { get; set; }
    public DateTime HieuLucDen { get; set; }
    public int SoLuotConLai { get; set; }
}

public class KiemTraVoucherRequest
{
    [Required(ErrorMessage = "Mã voucher không được để trống.")]
    [MaxLength(50, ErrorMessage = "Mã voucher tối đa 50 ký tự.")]
    public string MaCode { get; set; } = string.Empty;

    [Range(0, 10000000000, ErrorMessage = "Tổng tiền đặt phòng không hợp lệ.")]
    public decimal TongTienDatPhong { get; set; }
}

public class KiemTraVoucherResponse
{
    public bool HopLe { get; set; }
    public decimal TienGiam { get; set; }
    public string ThongBao { get; set; } = string.Empty;
}

public class TaoVoucherRequest
{
    [Required(ErrorMessage = "Mã code không được để trống.")]
    [MaxLength(50, ErrorMessage = "Mã code tối đa 50 ký tự.")]
    public string MaCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loại giảm giá không được để trống.")]
    [RegularExpression("^(phan_tram|so_tien|percent|fixed|co_dinh)$", ErrorMessage = "Loại giảm giá phải là 'percent'/'phan_tram' hoặc 'fixed'/'so_tien'.")]
    public string LoaiGiamGia { get; set; } = string.Empty;

    [Range(1, 1000000000, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0.")]
    public decimal GiaTriGiamGia { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Giá trị đặt phòng tối thiểu không được âm.")]
    public decimal? GiaTriDatPhongToiThieu { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Mức giảm tối đa không được âm.")]
    public decimal? GiamToiDa { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu hiệu lực không được để trống.")]
    public DateTime HieuLucTu { get; set; }

    [Required(ErrorMessage = "Ngày hết hạn hiệu lực không được để trống.")]
    public DateTime HieuLucDen { get; set; }

    [Range(1, 1000000, ErrorMessage = "Giới hạn sử dụng phải từ 1 đến 1,000,000 lượt.")]
    public int GioiHanSuDung { get; set; } = 100;
}
