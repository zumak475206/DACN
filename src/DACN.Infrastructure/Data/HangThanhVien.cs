using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class HangThanhVien
{
    public int MaId { get; set; }

    public string TenHang { get; set; } = null!;

    public int DiemToiThieu { get; set; }

    public decimal PhanTramGiamGia { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
