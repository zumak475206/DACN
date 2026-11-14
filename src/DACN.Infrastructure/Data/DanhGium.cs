using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DanhGium
{
    public int MaId { get; set; }

    public int MaNguoiDung { get; set; }

    public int MaLoaiPhong { get; set; }

    public int? MaDatPhong { get; set; }

    public int DiemDanhGia { get; set; }

    public string? BinhLuan { get; set; }

    public bool DaDuyet { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual DatPhong? MaDatPhongNavigation { get; set; }

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
