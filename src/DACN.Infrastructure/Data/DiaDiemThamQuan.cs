using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DiaDiemThamQuan
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public decimal? KhoangCachKm { get; set; }

    public string? MoTa { get; set; }

    public string? LienKetBanDo { get; set; }

    public decimal? ViDo { get; set; }

    public decimal? KinhDo { get; set; }

    public string? DiaChi { get; set; }

    public string? DuongDanAnh { get; set; }

    public string? PhongCach { get; set; }

    public bool DangHoatDong { get; set; }

    public virtual ICollection<GoiYdiaDiemTheoMuaAi> GoiYdiaDiemTheoMuaAis { get; set; } = new List<GoiYdiaDiemTheoMuaAi>();
}
