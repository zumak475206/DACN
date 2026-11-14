using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class MatMatHuHong
{
    public int MaId { get; set; }

    public int MaChiTietDatPhong { get; set; }

    public int? MaTaiSanPhong { get; set; }

    public int SoLuong { get; set; }

    public decimal TienPhat { get; set; }

    public string? MoTa { get; set; }

    public string? DuongDanAnh { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ChiTietDatPhong MaChiTietDatPhongNavigation { get; set; } = null!;

    public virtual TaiSanPhong? MaTaiSanPhongNavigation { get; set; }
}
