using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class ChiTietDonDichVu
{
    public int MaId { get; set; }

    public int MaDonDichVu { get; set; }

    public int MaDichVu { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public virtual DichVu MaDichVuNavigation { get; set; } = null!;

    public virtual DonDichVu MaDonDichVuNavigation { get; set; } = null!;
}
