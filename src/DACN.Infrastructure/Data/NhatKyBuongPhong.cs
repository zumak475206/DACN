using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class NhatKyBuongPhong
{
    public int MaId { get; set; }

    public int MaPhong { get; set; }

    public int? MaNhanVien { get; set; }

    public int? MaChiTietDatPhong { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime ThoiGianPhanCong { get; set; }

    public DateTime? ThoiGianBatDau { get; set; }

    public DateTime? ThoiGianHoanTat { get; set; }

    public string? GhiChuKiemTra { get; set; }

    public string? DanhSachAnh { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ChiTietDatPhong? MaChiTietDatPhongNavigation { get; set; }

    public virtual NguoiDung? MaNhanVienNavigation { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;
}
