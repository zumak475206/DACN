using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DichVu
{
    public int MaId { get; set; }

    public int MaDanhMucDichVu { get; set; }

    public string Ten { get; set; } = null!;

    public decimal GiaDichVu { get; set; }

    public string? DonViTinh { get; set; }

    public string? DuongDanAnh { get; set; }

    public string? MoTa { get; set; }

    public bool DangHoatDong { get; set; }

    public int? MaChiNhanh { get; set; }

    public virtual ICollection<ChiTietDonDichVu> ChiTietDonDichVus { get; set; } = new List<ChiTietDonDichVu>();

    public virtual DanhMucDichVu MaDanhMucDichVuNavigation { get; set; } = null!;

    public virtual ChiNhanh? MaChiNhanhNavigation { get; set; }
}
