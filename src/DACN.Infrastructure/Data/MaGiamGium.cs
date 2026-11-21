using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class MaGiamGium
{
    public int MaId { get; set; }

    public string MaCode { get; set; } = null!;

    public string LoaiGiamGia { get; set; } = null!;

    public decimal GiaTriGiamGia { get; set; }

    public decimal? GiaTriDatPhongToiThieu { get; set; }

    public decimal? GiamToiDa { get; set; }

    public DateTime HieuLucTu { get; set; }

    public DateTime HieuLucDen { get; set; }

    public int GioiHanSuDung { get; set; }

    public int SoLanDaDung { get; set; }

    public bool DangHoatDong { get; set; }

    public int? MaChiNhanh { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();

    public virtual ChiNhanh? MaChiNhanhNavigation { get; set; }
}
