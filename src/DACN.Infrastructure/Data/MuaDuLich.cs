using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class MuaDuLich
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public string LoaiMua { get; set; } = null!;

    public int ThangBatDau { get; set; }

    public int ThangKetThuc { get; set; }

    public string KhuVuc { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? MoTaThoiTiet { get; set; }

    public decimal? NhietDoTrungBinh { get; set; }

    public bool LaMuaCaoDiem { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ICollection<GoiYdiaDiemTheoMuaAi> GoiYdiaDiemTheoMuaAis { get; set; } = new List<GoiYdiaDiemTheoMuaAi>();
}
