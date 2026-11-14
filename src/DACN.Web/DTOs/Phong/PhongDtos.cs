using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.Phong;

public class LoaiPhongDto
{
    public int MaId { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string DuongDanRutGon { get; set; } = string.Empty;
    public decimal GiaCoBan { get; set; }
    public int SoNguoiLonToiDa { get; set; }
    public int SoTreEmToiDa { get; set; }
    public decimal? DienTichM2 { get; set; }
    public string? LoaiGiuong { get; set; }
    public string? HuongNhin { get; set; }
    public string? MoTa { get; set; }
    public bool DangHoatDong { get; set; }
    public int SoPhongTrong { get; set; }
    public List<string> HinhAnh { get; set; } = [];
    public List<string> TienNghi { get; set; } = [];
}

public class TaoLoaiPhongRequest
{
    [Required(ErrorMessage = "Tên loại phòng không được để trống.")]
    [MaxLength(255, ErrorMessage = "Tên loại phòng tối đa 255 ký tự.")]
    public string Ten { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn rút gọn không được để trống.")]
    [MaxLength(255, ErrorMessage = "Đường dẫn rút gọn tối đa 255 ký tự.")]
    public string DuongDanRutGon { get; set; } = string.Empty;

    [Range(0, 1000000000, ErrorMessage = "Giá cơ bản phải từ 0 đến 1,000,000,000 VNĐ.")]
    public decimal GiaCoBan { get; set; }

    [Range(1, 20, ErrorMessage = "Số người lớn tối đa từ 1 đến 20.")]
    public int SoNguoiLonToiDa { get; set; } = 2;

    [Range(0, 20, ErrorMessage = "Số trẻ em tối đa từ 0 đến 20.")]
    public int SoTreEmToiDa { get; set; } = 0;

    [Range(1, 1000, ErrorMessage = "Diện tích phòng từ 1 đến 1000 m2.")]
    public decimal? DienTichM2 { get; set; }

    [MaxLength(100, ErrorMessage = "Loại giường tối đa 100 ký tự.")]
    public string? LoaiGiuong { get; set; }

    [MaxLength(100, ErrorMessage = "Hướng nhìn tối đa 100 ký tự.")]
    public string? HuongNhin { get; set; }

    [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? MoTa { get; set; }

    public string? NoiDung { get; set; }
}

public class PhongDto
{
    public int MaId { get; set; }
    public int MaLoaiPhong { get; set; }
    public string TenLoaiPhong { get; set; } = string.Empty;
    public string SoPhong { get; set; } = string.Empty;
    public int? Tang { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string TrangThaiVeSinh { get; set; } = string.Empty;
    public string? SoMayLe { get; set; }
}

public class CapNhatTrangThaiPhongRequest
{
    [Required(ErrorMessage = "Trạng thái phòng không được để trống.")]
    [RegularExpression("^(trong|co_khach|bao_duong|don_dep|cho_don)$", ErrorMessage = "Trạng thái phòng phải là một trong các giá trị: trong, co_khach, bao_duong, don_dep, cho_don.")]
    public string TrangThai { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Trạng thái vệ sinh tối đa 50 ký tự.")]
    public string? TrangThaiVeSinh { get; set; }
}

public class TimKiemPhongRequest
{
    [Required(ErrorMessage = "Ngày nhận phòng không được để trống.")]
    public DateTime NgayNhanPhong { get; set; }

    [Required(ErrorMessage = "Ngày trả phòng không được để trống.")]
    public DateTime NgayTraPhong { get; set; }

    [Range(1, 20, ErrorMessage = "Số người lớn phải từ 1 đến 20.")]
    public int SoNguoiLon { get; set; } = 1;

    [Range(0, 20, ErrorMessage = "Số trẻ em phải từ 0 đến 20.")]
    public int SoTreEm { get; set; } = 0;
}
