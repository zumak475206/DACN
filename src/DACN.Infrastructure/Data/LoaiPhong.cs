using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class LoaiPhong
{
    public int MaId { get; set; }

    public string Ten { get; set; } = null!;

    public string DuongDanRutGon { get; set; } = null!;

    public decimal GiaCoBan { get; set; }

    public int SoNguoiLonToiDa { get; set; }

    public int SoTreEmToiDa { get; set; }

    public int? DienTichM2 { get; set; }

    public string? LoaiGiuong { get; set; }

    public string? HuongNhin { get; set; }

    public string? MoTa { get; set; }

    public string? NoiDung { get; set; }

    public bool DangHoatDong { get; set; }

    public int MaChiNhanh { get; set; } = 1;

    public DateTime NgayTao { get; set; }

    public virtual ICollection<AnhPhong> AnhPhongs { get; set; } = new List<AnhPhong>();

    public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; } = new List<ChiTietDatPhong>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<DuBaoNhuCauDatPhongAi> DuBaoNhuCauDatPhongAis { get; set; } = new List<DuBaoNhuCauDatPhongAi>();

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();

    public virtual ChiNhanh? MaChiNhanhNavigation { get; set; }

    public virtual ICollection<TienNghi> MaTienNghis { get; set; } = new List<TienNghi>();
}
