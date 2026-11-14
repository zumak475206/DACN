using System;

namespace DACN.Infrastructure.Data;

public partial class RefreshToken
{
    public int MaId { get; set; }

    public int MaNguoiDung { get; set; }

    public string Token { get; set; } = null!;

    public DateTime HetHanVao { get; set; }

    public DateTime NgayTao { get; set; }

    public string? DiaChiIpTao { get; set; }

    public bool DaThuHoi { get; set; }

    public DateTime? NgayThuHoi { get; set; }

    public string? LyDoThuHoi { get; set; }

    public string? ThayTheBoiToken { get; set; }

    public bool DaHetHan => DateTime.UtcNow >= HetHanVao;

    public bool ConHieuLuc => !DaThuHoi && !DaHetHan;

    public virtual NguoiDung MaNguoiDungNavigation { get; set; } = null!;
}
