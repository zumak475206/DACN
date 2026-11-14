using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DuDoanHuyPhongAi
{
    public int MaId { get; set; }

    public int MaDatPhong { get; set; }

    public decimal XacSuatHuyPhong { get; set; }

    public string MucDoRuiRo { get; set; } = null!;

    public string? YeuToChinh { get; set; }

    public DateTime ThoiGianDuDoan { get; set; }

    public bool? ThucTeDaHuy { get; set; }

    public string? PhienBanMoHinh { get; set; }

    public virtual DatPhong MaDatPhongNavigation { get; set; } = null!;
}
