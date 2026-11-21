using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class DatPhong
{
    public int MaId { get; set; }

    public string MaDatPhong { get; set; } = null!;

    public int? MaNguoiDung { get; set; }

    public int? NguoiTao { get; set; }

    public string? TenKhach { get; set; }

    public string? Sdtkhach { get; set; }

    public string? EmailKhach { get; set; }

    public int? MaGiamGia { get; set; }

    public int MaChiNhanh { get; set; } = 1;

    public string KenhDatPhong { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public decimal TongTien { get; set; }

    public decimal TienDatCoc { get; set; }

    public string? GhiChu { get; set; }

    public string? LyDoHuy { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; } = new List<ChiTietDatPhong>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<DuDoanHuyPhongAi> DuDoanHuyPhongAis { get; set; } = new List<DuDoanHuyPhongAi>();

    public virtual HoaDon? HoaDon { get; set; }

    public virtual ChiNhanh? MaChiNhanhNavigation { get; set; }

    public virtual MaGiamGium? MaGiamGiaNavigation { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }

    public virtual NguoiDung? NguoiTaoNavigation { get; set; }

    public virtual ICollection<PhienChatAi> PhienChatAis { get; set; } = new List<PhienChatAi>();
}
