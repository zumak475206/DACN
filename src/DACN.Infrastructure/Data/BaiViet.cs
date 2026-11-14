using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class BaiViet
{
    public int MaId { get; set; }

    public int? MaTacGia { get; set; }

    public string? DanhMuc { get; set; }

    public string TieuDe { get; set; } = null!;

    public string? DuongDanRutGon { get; set; }

    public string? NoiDung { get; set; }

    public string? DuongDanAnhBia { get; set; }

    public DateTime? NgayDang { get; set; }

    public bool DangHoatDong { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual NguoiDung? MaTacGiaNavigation { get; set; }
}
