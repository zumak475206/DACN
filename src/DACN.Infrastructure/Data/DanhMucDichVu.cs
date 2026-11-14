using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DanhMucDichVu
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public bool DangHoatDong { get; set; }

    public virtual ICollection<DichVu> DichVus { get; set; } = new List<DichVu>();
}
