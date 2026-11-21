using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class ChiNhanh
{
    public int MaId { get; set; }

    public string MaChiNhanhCode { get; set; } = null!;

    public string TenChiNhanh { get; set; } = null!;

    public string TinhThanh { get; set; } = null!;

    public string DiaChiChiTiet { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string EmailLienHe { get; set; } = null!;

    public string? HinhAnhDaiDien { get; set; }

    public int SoSao { get; set; } = 4;

    public decimal? ViDo { get; set; }

    public decimal? KinhDo { get; set; }

    public string VungMien { get; set; } = "central";

    public bool DangHoatDong { get; set; } = true;

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();

    public virtual ICollection<LoaiPhong> LoaiPhongs { get; set; } = new List<LoaiPhong>();

    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();

    public virtual ICollection<ChiPhiVanHanh> ChiPhiVanHanhs { get; set; } = new List<ChiPhiVanHanh>();

    public virtual ICollection<DuBaoNhuCauDatPhongAi> DuBaoNhuCauDatPhongAis { get; set; } = new List<DuBaoNhuCauDatPhongAi>();

    public virtual ICollection<DichVu> DichVus { get; set; } = new List<DichVu>();

    public virtual ICollection<MaGiamGium> MaGiamGia { get; set; } = new List<MaGiamGium>();

    public virtual ICollection<DiaDiemChiNhanh> DiaDiemChiNhanhs { get; set; } = new List<DiaDiemChiNhanh>();
}
