using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.Auth;

public class DangNhapRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [MaxLength(255, ErrorMessage = "Email tối đa 255 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string MatKhau { get; set; } = string.Empty;
}

public class DangKyRequest
{
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [MaxLength(255, ErrorMessage = "Họ tên tối đa 255 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [MaxLength(255, ErrorMessage = "Email tối đa 255 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự.")]
    public string MatKhau { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string? SoDienThoai { get; set; }
}

public class DoiMatKhauRequest
{
    [Required(ErrorMessage = "Mật khẩu cũ không được để trống.")]
    public string MatKhauCu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu mới tối đa 100 ký tự.")]
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class DangNhapResponse
{
    public int MaId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string LoaiNguoiDung { get; set; } = string.Empty;
    public int MaVaiTro { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public bool TrangThai { get; set; } = true;
    public List<string> DanhSachQuyen { get; set; } = new();
    public int TongDiem { get; set; }
    public string Token { get; set; } = string.Empty;
    public string LoaiToken { get; set; } = "Bearer";
    public DateTime HetHanVao { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenHetHanVao { get; set; }
}

public class CapNhatVaiTroNguoiDungRequest
{
    [Required]
    public int MaVaiTro { get; set; }
    public string? LoaiNguoiDung { get; set; }
}

public class CapNhatHoSoRequest
{
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [MaxLength(255, ErrorMessage = "Họ tên tối đa 255 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string? SoDienThoai { get; set; }
}

public class LamMoiTokenRequest
{
    [Required(ErrorMessage = "RefreshToken không được để trống.")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class DangXuatRequest
{
    public string? RefreshToken { get; set; }
}

public class QuenMatKhauRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;
}

public class DatLaiMatKhauRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã xác nhận không được để trống.")]
    public string MaXacNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu mới tối đa 100 ký tự.")]
    public string MatKhauMoi { get; set; } = string.Empty;
}
