using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class QuyenHan
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public string? PhanHe { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<VaiTro> MaVaiTros { get; set; } = new List<VaiTro>();
}
