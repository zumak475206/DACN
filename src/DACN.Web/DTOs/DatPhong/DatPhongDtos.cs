using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.DatPhong;

public class TaoDatPhongRequest
{
    public int? MaNguoiDung { get; set; }
    public int? MaChiNhanh { get; set; }

    [MaxLength(255, ErrorMessage = "Tên khách hàng tối đa 255 ký tự.")]
    public string? TenKhach { get; set; }

    [Phone(ErrorMessage = "Số điện thoại khách hàng không hợp lệ.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string? SdtKhach { get; set; }

    [EmailAddress(ErrorMessage = "Email khách hàng không hợp lệ.")]
    [MaxLength(255, ErrorMessage = "Email tối đa 255 ký tự.")]
    public string? EmailKhach { get; set; }

    [MaxLength(50, ErrorMessage = "Mã giảm giá tối đa 50 ký tự.")]
    public string? MaGiamGia { get; set; }

    [MaxLength(50, ErrorMessage = "Kênh đặt phòng tối đa 50 ký tự.")]
    public string KenhDatPhong { get; set; } = "website";

    [MaxLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
    public string? GhiChu { get; set; }

    [Required(ErrorMessage = "Danh sách phòng không được để trống.")]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất một phòng.")]
    public List<ChiTietDatPhongRequest> DanhSachPhong { get; set; } = [];
}

public class ChiTietDatPhongRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã loại phòng không hợp lệ.")]
    public int MaLoaiPhong { get; set; }

    [Required(ErrorMessage = "Ngày nhận phòng không được để trống.")]
    public DateTime NgayNhanPhong { get; set; }

    [Required(ErrorMessage = "Ngày trả phòng không được để trống.")]
    public DateTime NgayTraPhong { get; set; }
}

public class HuyDatPhongRequest
{
    [Required(ErrorMessage = "Lý do hủy đặt phòng không được để trống.")]
    [MaxLength(500, ErrorMessage = "Lý do hủy tối đa 500 ký tự.")]
    public string LyDoHuy { get; set; } = string.Empty;
}

public class GanPhongRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã phòng không hợp lệ.")]
    public int MaPhong { get; set; }
}

public class TraCuuDatPhongRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã đặt phòng.")]
    [MaxLength(50, ErrorMessage = "Mã đặt phòng tối đa 50 ký tự.")]
    public string MaDatPhong { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại hoặc email đặt phòng.")]
    [MaxLength(255, ErrorMessage = "Thông tin liên hệ tối đa 255 ký tự.")]
    public string ThongTinLienHe { get; set; } = string.Empty;
}

public class DatPhongDto
{
    public int MaId { get; set; }
    public string MaDatPhong { get; set; } = string.Empty;
    public int? MaChiNhanh { get; set; }
    public string? TenChiNhanh { get; set; }
    public string? TenKhach { get; set; }
    public string? SdtKhach { get; set; }
    public string? EmailKhach { get; set; }
    public string KenhDatPhong { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public decimal TongTien { get; set; }
    public decimal TienDatCoc { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
    public List<ChiTietDatPhongDto> DanhSachPhong { get; set; } = [];
}

public class ChiTietDatPhongDto
{
    public int MaId { get; set; }
    public int? MaPhong { get; set; }
    public string? SoPhong { get; set; }
    public int MaLoaiPhong { get; set; }
    public string TenLoaiPhong { get; set; } = string.Empty;
    public DateTime NgayNhanPhongDuKien { get; set; }
    public DateTime NgayTraPhongDuKien { get; set; }
    public decimal GiaMoiDem { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime? ThoiGianNhanPhongThucTe { get; set; }
    public DateTime? ThoiGianTraPhongThucTe { get; set; }
}
