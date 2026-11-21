using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class Phong
{
    public int MaId { get; set; }

    public int MaLoaiPhong { get; set; }

    public int MaChiNhanh { get; set; } = 1;

    public string SoPhong { get; set; } = null!;

    public int? Tang { get; set; }

    public string TrangThai { get; set; } = null!;

    public string TrangThaiVeSinh { get; set; } = null!;

    public string? SoMayLe { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; } = new List<ChiTietDatPhong>();

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;

    public virtual ChiNhanh? MaChiNhanhNavigation { get; set; }

    public virtual ICollection<NhatKyBuongPhong> NhatKyBuongPhongs { get; set; } = new List<NhatKyBuongPhong>();

    public virtual ICollection<TaiSanPhong> TaiSanPhongs { get; set; } = new List<TaiSanPhong>();
}
