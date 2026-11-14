using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class TinNhanChatAi
{
    public int MaId { get; set; }

    public int MaPhienChat { get; set; }

    public string NguoiGui { get; set; } = null!;

    public string NoiDungTinNhan { get; set; } = null!;

    public string LoaiTinNhan { get; set; } = null!;

    public string? DuLieuKemTheo { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual PhienChatAi MaPhienChatNavigation { get; set; } = null!;
}
