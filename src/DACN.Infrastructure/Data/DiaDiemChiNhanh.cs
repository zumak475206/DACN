namespace DACN.Infrastructure.Data;

public partial class DiaDiemChiNhanh
{
    public int MaDiaDiem { get; set; }

    public int MaChiNhanh { get; set; }

    public decimal KhoangCachKm { get; set; }

    public int ThoiGianDiChuyenPhut { get; set; }

    public bool LaChiNhanhGanNhat { get; set; } = true;

    public virtual DiaDiemThamQuan MaDiaDiemNavigation { get; set; } = null!;

    public virtual ChiNhanh MaChiNhanhNavigation { get; set; } = null!;
}
