using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class ThanhToan
{
    public int MaId { get; set; }

    public int MaHoaDon { get; set; }

    public string PhuongThucThanhToan { get; set; } = null!;

    public decimal SoTienDaThanhToan { get; set; }

    public string? MaGiaoDich { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayThanhToan { get; set; }

    public virtual HoaDon MaHoaDonNavigation { get; set; } = null!;
}
