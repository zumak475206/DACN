using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class TienNghi
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public string? DuongDanBieuTuong { get; set; }

    public bool DangHoatDong { get; set; }

    public virtual ICollection<LoaiPhong> MaLoaiPhongs { get; set; } = new List<LoaiPhong>();
}
