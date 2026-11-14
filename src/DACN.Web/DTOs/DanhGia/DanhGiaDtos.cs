using System.ComponentModel.DataAnnotations;

namespace DACN.Web.DTOs.DanhGia;

public class DanhGiaDto
{
    public int MaId { get; set; }
    public string TenNguoiDung { get; set; } = string.Empty;
    public int DiemDanhGia { get; set; }
    public string? BinhLuan { get; set; }
    public bool DaDuyet { get; set; }
    public DateTime NgayTao { get; set; }
    public int MaLoaiPhong { get; set; }
    public string TenLoaiPhong { get; set; } = string.Empty;
}

public class TaoDanhGiaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã loại phòng không hợp lệ.")]
    public int MaLoaiPhong { get; set; }

    public int? MaDatPhong { get; set; }

    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
    public int DiemDanhGia { get; set; }

    [MaxLength(1000, ErrorMessage = "Bình luận tối đa 1000 ký tự.")]
    public string? BinhLuan { get; set; }
}
