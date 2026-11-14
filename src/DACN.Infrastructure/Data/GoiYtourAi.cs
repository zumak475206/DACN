using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class GoiYtourAi
{
    public int MaId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string? MoTa { get; set; }

    public string DiemDen { get; set; } = null!;

    public decimal GiaToiThieu { get; set; }

    public decimal GiaToiDa { get; set; }

    public int SoNgay { get; set; }

    public string MuaDuLich { get; set; } = null!;

    public string? DoiTuongPhuHop { get; set; }

    public string? CacThangTotNhat { get; set; }

    public string? DuongDanAnh { get; set; }

    public string? LienHeDoiTac { get; set; }

    public decimal? DiemDanhGiaTrungBinh { get; set; }

    public bool DangHoatDong { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }
}
