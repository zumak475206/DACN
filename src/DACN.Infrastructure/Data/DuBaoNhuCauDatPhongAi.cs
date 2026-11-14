using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DuBaoNhuCauDatPhongAi
{
    public int MaId { get; set; }

    public DateOnly NgayDuBao { get; set; }

    public int? MaLoaiPhong { get; set; }

    public int SoDatPhongDuBao { get; set; }

    public decimal? TyLeLapDayDuBao { get; set; }

    public string PhienBanMoHinh { get; set; } = null!;

    public DateTime ThoiGianDuDoan { get; set; }

    public string? GhiChuDuBao { get; set; }

    public virtual LoaiPhong? MaLoaiPhongNavigation { get; set; }
}
