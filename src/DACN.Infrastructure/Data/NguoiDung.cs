using System;
using System.Collections.Generic;

namespace DACN.Infrastructure.Data;

public partial class NguoiDung
{
    public int MaId { get; set; }

    public int MaVaiTro { get; set; }

    public int? MaHangThanhVien { get; set; }

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string MaBamMatKhau { get; set; } = null!;

    public string LoaiNguoiDung { get; set; } = null!;

    public bool TrangThai { get; set; }

    public string? DuongDanAnhDaiDien { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? DiaChi { get; set; }

    public int TongDiem { get; set; }

    public string? SoThich { get; set; }

    public int? MaChiNhanh { get; set; } = 1;

    public DateTime NgayTao { get; set; }

    public virtual ICollection<BaiViet> BaiViets { get; set; } = new List<BaiViet>();

    public virtual ICollection<ChiPhiVanHanh> ChiPhiVanHanhNguoiGhiNhanNavigations { get; set; } = new List<ChiPhiVanHanh>();

    public virtual ICollection<ChiPhiVanHanh> ChiPhiVanHanhNguoiPheDuyetNavigations { get; set; } = new List<ChiPhiVanHanh>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<DatPhong> DatPhongMaNguoiDungNavigations { get; set; } = new List<DatPhong>();

    public virtual ICollection<DatPhong> DatPhongNguoiTaoNavigations { get; set; } = new List<DatPhong>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual HangThanhVien? MaHangThanhVienNavigation { get; set; }

    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;

    public virtual ChiNhanh? MaChiNhanhNavigation { get; set; }

    public virtual ICollection<NhatKyBuongPhong> NhatKyBuongPhongs { get; set; } = new List<NhatKyBuongPhong>();

    public virtual ICollection<PhienChatAi> PhienChatAis { get; set; } = new List<PhienChatAi>();

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<NhatKyHeThong> NhatKyHeThongs { get; set; } = new List<NhatKyHeThong>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
