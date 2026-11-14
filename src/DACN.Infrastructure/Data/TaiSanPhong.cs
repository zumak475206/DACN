using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class TaiSanPhong
{
    public int MaId { get; set; }

    public int MaPhong { get; set; }

    public string TenTaiSan { get; set; } = null!;

    public int SoLuong { get; set; }

    public decimal GiaDenBu { get; set; }

    public string? LoaiTaiSan { get; set; }

    public string? GhiChu { get; set; }

    public bool DangHoatDong { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;

    public virtual ICollection<MatMatHuHong> MatMatHuHongs { get; set; } = new List<MatMatHuHong>();
}
