using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class ChiTietDatPhong
{
    public int MaId { get; set; }

    public int MaDatPhong { get; set; }

    public int? MaPhong { get; set; }

    public int MaLoaiPhong { get; set; }

    public DateTime NgayNhanPhongDuKien { get; set; }

    public DateTime NgayTraPhongDuKien { get; set; }

    public decimal GiaMoiDem { get; set; }

    public DateTime? ThoiGianNhanPhongThucTe { get; set; }

    public DateTime? ThoiGianTraPhongThucTe { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<DonDichVu> DonDichVus { get; set; } = new List<DonDichVu>();

    public virtual DatPhong MaDatPhongNavigation { get; set; } = null!;

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;

    public virtual Phong? MaPhongNavigation { get; set; }

    public virtual ICollection<MatMatHuHong> MatMatHuHongs { get; set; } = new List<MatMatHuHong>();

    public virtual ICollection<NhatKyBuongPhong> NhatKyBuongPhongs { get; set; } = new List<NhatKyBuongPhong>();
}
