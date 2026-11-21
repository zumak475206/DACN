using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DiaDiemThamQuan
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public decimal? KhoangCachKm { get; set; }

    public string? MoTa { get; set; }

    public string? LienKetBanDo { get; set; }

    public decimal? ViDo { get; set; }

    public decimal? KinhDo { get; set; }

    public string? DiaChi { get; set; }

    public string? DuongDanAnh { get; set; }

    public string? PhongCach { get; set; }

    public bool DangHoatDong { get; set; }

    public string? TinhThanh { get; set; }

    public string? KhuVuc { get; set; }

    public string? TheLoai { get; set; }

    public bool NgoaiTroi { get; set; } = true;

    public bool LaBienDao { get; set; } = false;

    public string PhuongTienTiepCan { get; set; } = "duong_bo";

    public int ThangDepTu { get; set; } = 1;

    public int ThangDepDen { get; set; } = 12;

    public int? ThoiGianThamQuanPhut { get; set; }

    public decimal? GiaVeThamKhao { get; set; }

    public string? GioMoCua { get; set; }

    public decimal DiemBienTap { get; set; } = 4.50m;

    public string? NguonDuLieu { get; set; }

    public string? MaNguonNgoai { get; set; }

    public DateTime? NgayXacMinh { get; set; }

    public virtual ICollection<GoiYdiaDiemTheoMuaAi> GoiYdiaDiemTheoMuaAis { get; set; } = new List<GoiYdiaDiemTheoMuaAi>();

    public virtual ICollection<DiaDiemChiNhanh> DiaDiemChiNhanhs { get; set; } = new List<DiaDiemChiNhanh>();
}
