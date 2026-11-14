using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class PhienChatAi
{
    public int MaId { get; set; }

    public int? MaNguoiDung { get; set; }

    public int? MaDatPhong { get; set; }

    public string MaPhienChat { get; set; } = null!;

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime? ThoiGianKetThuc { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual DatPhong? MaDatPhongNavigation { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }

    public virtual ICollection<TinNhanChatAi> TinNhanChatAis { get; set; } = new List<TinNhanChatAi>();
}
