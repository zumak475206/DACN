using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DonDichVu
{
    public int MaId { get; set; }

    public int MaChiTietDatPhong { get; set; }

    public DateTime NgayDatDichVu { get; set; }

    public decimal TongTien { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<ChiTietDonDichVu> ChiTietDonDichVus { get; set; } = new List<ChiTietDonDichVu>();

    public virtual ChiTietDatPhong MaChiTietDatPhongNavigation { get; set; } = null!;
}
