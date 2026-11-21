using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class ChiPhiVanHanh
{
    public int MaId { get; set; }

    public string DanhMuc { get; set; } = null!;

    public string? MoTa { get; set; }

    public decimal SoTien { get; set; }

    public DateOnly NgayChi { get; set; }

    public int NguoiGhiNhan { get; set; }

    public int? NguoiPheDuyet { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? DuongDanBienLai { get; set; }

    public int MaChiNhanh { get; set; } = 1;

    public DateTime NgayTao { get; set; }

    public virtual NguoiDung NguoiGhiNhanNavigation { get; set; } = null!;

    public virtual NguoiDung? NguoiPheDuyetNavigation { get; set; }

    public virtual ChiNhanh? MaChiNhanhNavigation { get; set; }
}
