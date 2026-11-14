using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class HoaDon
{
    public int MaId { get; set; }

    public int MaDatPhong { get; set; }

    public int? NguoiLapHoaDon { get; set; }

    public decimal TongTienPhong { get; set; }

    public decimal TongTienDichVu { get; set; }

    public decimal TienGiamGia { get; set; }

    public decimal TienThue { get; set; }

    public decimal TienPhuThu { get; set; }

    public string? LyDoPhuThu { get; set; }

    public decimal TongThanhToan { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual DatPhong MaDatPhongNavigation { get; set; } = null!;

    public virtual NguoiDung? NguoiLapHoaDonNavigation { get; set; }

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
