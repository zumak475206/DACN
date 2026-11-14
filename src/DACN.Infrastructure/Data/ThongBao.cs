using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class ThongBao
{
    public int MaId { get; set; }

    public int MaNguoiDung { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string? LoaiThongBao { get; set; }

    public string? LienKetThamChieu { get; set; }

    public bool DaDoc { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
