using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class VaiTro
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();

    public virtual ICollection<QuyenHan> MaQuyenHans { get; set; } = new List<QuyenHan>();
}
