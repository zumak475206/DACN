using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class GoiYdiaDiemTheoMuaAi
{
    public int MaId { get; set; }

    public int MaDiaDiem { get; set; }

    public int MaMua { get; set; }

    public string? MuaDuLich { get; set; }

    public int ThangBatDauGoiY { get; set; }

    public int ThangKetThucGoiY { get; set; }

    public decimal DiemPhuHop { get; set; }

    public string? LyDoGoiY { get; set; }

    public string? LoaiThoiTiet { get; set; }

    public bool NoiBat { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual DiaDiemThamQuan MaDiaDiemNavigation { get; set; } = null!;

    public virtual MuaDuLich MaMuaNavigation { get; set; } = null!;
}
