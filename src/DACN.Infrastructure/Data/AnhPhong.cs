using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class AnhPhong
{
    public int MaId { get; set; }

    public int MaLoaiPhong { get; set; }

    public string DuongDanAnh { get; set; } = null!;

    public bool LaAnhChinh { get; set; }

    public bool DangHoatDong { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;
}
