using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DACN.Infrastructure.Data;

public partial class DACNContext : DbContext
{
    public DACNContext(DbContextOptions<DACNContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AnhPhong> AnhPhongs { get; set; }

    public virtual DbSet<BaiViet> BaiViets { get; set; }

    public virtual DbSet<ChiPhiVanHanh> ChiPhiVanHanhs { get; set; }

    public virtual DbSet<ChiTietDatPhong> ChiTietDatPhongs { get; set; }

    public virtual DbSet<ChiTietDonDichVu> ChiTietDonDichVus { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<DanhMucDichVu> DanhMucDichVus { get; set; }

    public virtual DbSet<DatPhong> DatPhongs { get; set; }

    public virtual DbSet<DiaDiemThamQuan> DiaDiemThamQuans { get; set; }

    public virtual DbSet<DichVu> DichVus { get; set; }

    public virtual DbSet<DonDichVu> DonDichVus { get; set; }

    public virtual DbSet<DuBaoNhuCauDatPhongAi> DuBaoNhuCauDatPhongAis { get; set; }

    public virtual DbSet<DuDoanHuyPhongAi> DuDoanHuyPhongAis { get; set; }

    public virtual DbSet<GoiYdiaDiemTheoMuaAi> GoiYdiaDiemTheoMuaAis { get; set; }

    public virtual DbSet<GoiYtourAi> GoiYtourAis { get; set; }

    public virtual DbSet<HangThanhVien> HangThanhViens { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }

    public virtual DbSet<MaGiamGium> MaGiamGia { get; set; }

    public virtual DbSet<MatMatHuHong> MatMatHuHongs { get; set; }

    public virtual DbSet<MuaDuLich> MuaDuLiches { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NhatKyBuongPhong> NhatKyBuongPhongs { get; set; }

    public virtual DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<PhienChatAi> PhienChatAis { get; set; }

    public virtual DbSet<Phong> Phongs { get; set; }

    public virtual DbSet<QuyenHan> QuyenHans { get; set; }

    public virtual DbSet<TaiSanPhong> TaiSanPhongs { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<TienNghi> TienNghis { get; set; }

    public virtual DbSet<TinNhanChatAi> TinNhanChatAis { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnhPhong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Room_Images");

            entity.ToTable("AnhPhong");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.AnhPhongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .HasConstraintName("FK_RoomImages_RoomTypes");
        });

        modelBuilder.Entity<BaiViet>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Articles");

            entity.ToTable("BaiViet");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.DanhMuc)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DuongDanRutGon).HasMaxLength(255);
            entity.Property(e => e.NgayDang).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaTacGiaNavigation).WithMany(p => p.BaiViets)
                .HasForeignKey(d => d.MaTacGia)
                .HasConstraintName("FK_Articles_Users");
        });

        modelBuilder.Entity<ChiPhiVanHanh>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Operating_Expenses");

            entity.ToTable("ChiPhiVanHanh");

            entity.HasIndex(e => new { e.NgayChi, e.TrangThai }, "IX_OperatingExpenses_date_status");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DanhMuc)
                .HasMaxLength(100)
                .IsUnicode(true);
            entity.Property(e => e.NgayChi).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.NguoiGhiNhanNavigation).WithMany(p => p.ChiPhiVanHanhNguoiGhiNhanNavigations)
                .HasForeignKey(d => d.NguoiGhiNhan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperatingExpenses_RecordedBy");

            entity.HasOne(d => d.NguoiPheDuyetNavigation).WithMany(p => p.ChiPhiVanHanhNguoiPheDuyetNavigations)
                .HasForeignKey(d => d.NguoiPheDuyet)
                .HasConstraintName("FK_OperatingExpenses_ApprovedBy");
        });

        modelBuilder.Entity<ChiTietDatPhong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Booking_Details");

            entity.ToTable("ChiTietDatPhong");

            entity.HasIndex(e => e.MaDatPhong, "IX_BookingDetails_booking_id");

            entity.HasIndex(e => new { e.NgayNhanPhongDuKien, e.NgayTraPhongDuKien }, "IX_BookingDetails_dates");

            entity.HasIndex(e => e.MaPhong, "IX_BookingDetails_room_id");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.GiaMoiDem).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayNhanPhongDuKien).HasColumnType("datetime");
            entity.Property(e => e.NgayTraPhongDuKien).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianNhanPhongThucTe).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianTraPhongThucTe).HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.ChiTietDatPhongs)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK_BookingDetails_Bookings");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.ChiTietDatPhongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetails_RoomTypes");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.ChiTietDatPhongs)
                .HasForeignKey(d => d.MaPhong)
                .HasConstraintName("FK_BookingDetails_Rooms");
        });

        modelBuilder.Entity<ChiTietDonDichVu>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Order_Service_Details");

            entity.ToTable("ChiTietDonDichVu");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaDichVuNavigation).WithMany(p => p.ChiTietDonDichVus)
                .HasForeignKey(d => d.MaDichVu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderServiceDetails_Services");

            entity.HasOne(d => d.MaDonDichVuNavigation).WithMany(p => p.ChiTietDonDichVus)
                .HasForeignKey(d => d.MaDonDichVu)
                .HasConstraintName("FK_OrderServiceDetails_Orders");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Reviews");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK_Reviews_Bookings");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_RoomTypes");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Users");
        });

        modelBuilder.Entity<DanhMucDichVu>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Service_Categories");

            entity.ToTable("DanhMucDichVu");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.Ten).HasMaxLength(255);
        });

        modelBuilder.Entity<DatPhong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Bookings");

            entity.ToTable("DatPhong");

            entity.HasIndex(e => e.NguoiTao, "IX_Bookings_created_by");

            entity.HasIndex(e => e.TrangThai, "IX_Bookings_status");

            entity.HasIndex(e => e.MaNguoiDung, "IX_Bookings_user_id");

            entity.HasIndex(e => e.MaDatPhong, "UQ_Bookings_booking_code").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.EmailKhach).HasMaxLength(255);
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.KenhDatPhong)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("web");
            entity.Property(e => e.LyDoHuy).HasMaxLength(500);
            entity.Property(e => e.MaDatPhong).HasMaxLength(50);
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Sdtkhach)
                .HasMaxLength(50)
                .HasColumnName("SDTKhach");
            entity.Property(e => e.TenKhach).HasMaxLength(255);
            entity.Property(e => e.TienDatCoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.MaGiamGiaNavigation).WithMany(p => p.DatPhongs)
                .HasForeignKey(d => d.MaGiamGia)
                .HasConstraintName("FK_Bookings_Vouchers");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.DatPhongMaNguoiDungNavigations)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_Bookings_Users");

            entity.HasOne(d => d.NguoiTaoNavigation).WithMany(p => p.DatPhongNguoiTaoNavigations)
                .HasForeignKey(d => d.NguoiTao)
                .HasConstraintName("FK_Bookings_CreatedBy");
        });

        modelBuilder.Entity<DiaDiemThamQuan>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Attractions");

            entity.ToTable("DiaDiemThamQuan");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.DiaChi).HasMaxLength(500);
            entity.Property(e => e.KhoangCachKm)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("KhoangCach_km");
            entity.Property(e => e.KinhDo).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PhongCach).HasMaxLength(200);
            entity.Property(e => e.Ten).HasMaxLength(255);
            entity.Property(e => e.ViDo).HasColumnType("decimal(10, 8)");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Services");

            entity.ToTable("DichVu");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.DonViTinh).HasMaxLength(50);
            entity.Property(e => e.GiaDichVu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Ten).HasMaxLength(255);

            entity.HasOne(d => d.MaDanhMucDichVuNavigation).WithMany(p => p.DichVus)
                .HasForeignKey(d => d.MaDanhMucDichVu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Services_Categories");
        });

        modelBuilder.Entity<DonDichVu>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Order_Services");

            entity.ToTable("DonDichVu");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.NgayDatDichVu)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.MaChiTietDatPhongNavigation).WithMany(p => p.DonDichVus)
                .HasForeignKey(d => d.MaChiTietDatPhong)
                .HasConstraintName("FK_OrderServices_BookingDetails");
        });

        modelBuilder.Entity<DuBaoNhuCauDatPhongAi>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_AI_Booking_Demand_Predictions");

            entity.ToTable("DuBaoNhuCauDatPhong_AI");

            entity.HasIndex(e => e.NgayDuBao, "IX_AIBookingDemand_date");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.PhienBanMoHinh)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("moving-average-v1");
            entity.Property(e => e.ThoiGianDuDoan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TyLeLapDayDuBao).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.DuBaoNhuCauDatPhongAis)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AIBookingDemand_RoomTypes");
        });

        modelBuilder.Entity<DuDoanHuyPhongAi>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_AI_Cancellation_Predictions");

            entity.ToTable("DuDoanHuyPhong_AI");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.MucDoRuiRo)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PhienBanMoHinh)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("rule-based-v1");
            entity.Property(e => e.ThoiGianDuDoan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.XacSuatHuyPhong).HasColumnType("decimal(5, 4)");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.DuDoanHuyPhongAis)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK_AICancellation_Bookings");
        });

        modelBuilder.Entity<GoiYdiaDiemTheoMuaAi>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_AI_Seasonal_Attractions");

            entity.ToTable("GoiYDiaDiemTheoMua_AI");

            entity.HasIndex(e => e.MaMua, "IX_AISeasonal_season_id");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DiemPhuHop)
                .HasDefaultValue(500m)
                .HasColumnType("decimal(5, 3)");
            entity.Property(e => e.LoaiThoiTiet)
                .HasMaxLength(50);
            entity.Property(e => e.LyDoGoiY).HasMaxLength(500);
            entity.Property(e => e.MuaDuLich)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaDiaDiemNavigation).WithMany(p => p.GoiYdiaDiemTheoMuaAis)
                .HasForeignKey(d => d.MaDiaDiem)
                .HasConstraintName("FK_AISeasonalAttractions_Attractions");

            entity.HasOne(d => d.MaMuaNavigation).WithMany(p => p.GoiYdiaDiemTheoMuaAis)
                .HasForeignKey(d => d.MaMua)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AISeasonalAttractions_Seasons");
        });

        modelBuilder.Entity<GoiYtourAi>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_AI_Tour_Suggestions");

            entity.ToTable("GoiYTour_AI");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.CacThangTotNhat).HasMaxLength(100);
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.DiemDanhGiaTrungBinh).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.DiemDen).HasMaxLength(255);
            entity.Property(e => e.GiaToiDa).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaToiThieu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LienHeDoiTac).HasMaxLength(255);
            entity.Property(e => e.MuaDuLich)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhongCach).HasMaxLength(200);
            entity.Property(e => e.TieuDe).HasMaxLength(255);
        });

        modelBuilder.Entity<HangThanhVien>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Memberships");

            entity.ToTable("HangThanhVien");

            entity.HasIndex(e => e.TenHang, "UQ_Memberships_tier_name").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.PhanTramGiamGia).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TenHang).HasMaxLength(100);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Invoices");

            entity.ToTable("HoaDon");

            entity.HasIndex(e => e.MaDatPhong, "IX_Invoices_booking_id");

            entity.HasIndex(e => e.MaDatPhong, "UQ_HoaDon_MaDatPhong").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.LyDoPhuThu).HasMaxLength(255);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TienGiamGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TienPhuThu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TienThue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongThanhToan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTienDichVu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTienPhong).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.MaDatPhongNavigation).WithOne(p => p.HoaDon)
                .HasForeignKey<HoaDon>(d => d.MaDatPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Bookings");

            entity.HasOne(d => d.NguoiLapHoaDonNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.NguoiLapHoaDon)
                .HasConstraintName("FK_Invoices_IssuedBy");
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Room_Types");

            entity.ToTable("LoaiPhong");

            entity.HasIndex(e => e.DuongDanRutGon, "UQ_Room_Types_slug").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.DienTichM2).HasColumnName("DienTich_m2");
            entity.Property(e => e.DuongDanRutGon)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.GiaCoBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HuongNhin).HasMaxLength(100);
            entity.Property(e => e.LoaiGiuong).HasMaxLength(100);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoNguoiLonToiDa).HasDefaultValue(2);
            entity.Property(e => e.SoTreEmToiDa).HasDefaultValue(1);
            entity.Property(e => e.Ten).HasMaxLength(255);

            entity.HasMany(d => d.MaTienNghis).WithMany(p => p.MaLoaiPhongs)
                .UsingEntity<Dictionary<string, object>>(
                    "TienNghiLoaiPhong",
                    r => r.HasOne<TienNghi>().WithMany()
                        .HasForeignKey("MaTienNghi")
                        .HasConstraintName("FK_RoomTypeAmenities_Amenities"),
                    l => l.HasOne<LoaiPhong>().WithMany()
                        .HasForeignKey("MaLoaiPhong")
                        .HasConstraintName("FK_RoomTypeAmenities_RoomTypes"),
                    j =>
                    {
                        j.HasKey("MaLoaiPhong", "MaTienNghi").HasName("PK_RoomType_Amenities");
                        j.ToTable("TienNghi_LoaiPhong");
                    });
        });

        modelBuilder.Entity<MaGiamGium>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Vouchers");

            entity.HasIndex(e => e.MaCode, "UQ_Vouchers_code").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.GiaTriDatPhongToiThieu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiamToiDa).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaTriGiamGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GioiHanSuDung).HasDefaultValue(100);
            entity.Property(e => e.HieuLucDen).HasColumnType("datetime");
            entity.Property(e => e.HieuLucTu).HasColumnType("datetime");
            entity.Property(e => e.LoaiGiamGia)
                .HasMaxLength(50)
                .HasDefaultValue("percent");
            entity.Property(e => e.MaCode).HasMaxLength(50);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<MatMatHuHong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Loss_And_Damages");

            entity.ToTable("MatMatHuHong");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);
            entity.Property(e => e.TienPhat).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaChiTietDatPhongNavigation).WithMany(p => p.MatMatHuHongs)
                .HasForeignKey(d => d.MaChiTietDatPhong)
                .HasConstraintName("FK_LossAndDamages_BookingDetails");

            entity.HasOne(d => d.MaTaiSanPhongNavigation).WithMany(p => p.MatMatHuHongs)
                .HasForeignKey(d => d.MaTaiSanPhong)
                .HasConstraintName("FK_LossAndDamages_RoomInventory");
        });

        modelBuilder.Entity<MuaDuLich>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Seasons");

            entity.ToTable("MuaDuLich");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.KhuVuc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("all");
            entity.Property(e => e.LoaiMua)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MoTaThoiTiet).HasMaxLength(500);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NhietDoTrungBinh).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Users");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.Email, "UQ_Users_email").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DiaChi).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.HoTen).HasMaxLength(255);
            entity.Property(e => e.LoaiNguoiDung)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("customer");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai).HasMaxLength(50);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaHangThanhVienNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.MaHangThanhVien)
                .HasConstraintName("FK_Users_Memberships");

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<NhatKyBuongPhong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Housekeeping_Logs");

            entity.ToTable("NhatKyBuongPhong");

            entity.HasIndex(e => e.TrangThai, "IX_Housekeeping_status");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.GhiChuKiemTra).HasMaxLength(500);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ThoiGianBatDau).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianHoanTat).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianPhanCong)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("assigned");

            entity.HasOne(d => d.MaChiTietDatPhongNavigation).WithMany(p => p.NhatKyBuongPhongs)
                .HasForeignKey(d => d.MaChiTietDatPhong)
                .HasConstraintName("FK_Housekeeping_BookingDetails");

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.NhatKyBuongPhongs)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK_Housekeeping_Staff");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.NhatKyBuongPhongs)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Housekeeping_Rooms");
        });

        modelBuilder.Entity<PhienChatAi>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Chat_Sessions");

            entity.ToTable("PhienChat_AI");

            entity.HasIndex(e => e.MaPhienChat, "UQ_Chat_Sessions_token").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.MaPhienChat)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ThoiGianBatDau)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ThoiGianKetThuc).HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.PhienChatAis)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK_ChatSessions_Bookings");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.PhienChatAis)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_ChatSessions_Users");
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Rooms");

            entity.ToTable("Phong");

            entity.HasIndex(e => e.MaLoaiPhong, "IX_Rooms_room_type_id");

            entity.HasIndex(e => new { e.TrangThai, e.TrangThaiVeSinh }, "IX_Rooms_status_cleaning");

            entity.HasIndex(e => e.SoPhong, "UQ_Rooms_room_number").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoMayLe)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SoPhong).HasMaxLength(50);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Available");
            entity.Property(e => e.TrangThaiVeSinh)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Clean");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_RoomTypes");
        });

        modelBuilder.Entity<QuyenHan>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Permissions");

            entity.ToTable("QuyenHan");

            entity.HasIndex(e => e.Ten, "UQ_Permissions_name").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.PhanHe).HasMaxLength(100);
            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<TaiSanPhong>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Room_Inventory");

            entity.ToTable("TaiSanPhong");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.GiaDenBu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LoaiTaiSan)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SoLuong).HasDefaultValue(1);
            entity.Property(e => e.TenTaiSan).HasMaxLength(255);

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.TaiSanPhongs)
                .HasForeignKey(d => d.MaPhong)
                .HasConstraintName("FK_RoomInventory_Rooms");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Payments");

            entity.ToTable("ThanhToan");

            entity.HasIndex(e => e.MaHoaDon, "IX_Payments_invoice_id");

            entity.HasIndex(e => e.TrangThai, "IX_Payments_status");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.MaGiaoDich).HasMaxLength(100);
            entity.Property(e => e.NgayThanhToan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhuongThucThanhToan)
                .HasMaxLength(50)
                .HasDefaultValue("Cash");
            entity.Property(e => e.SoTienDaThanhToan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Success");

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.MaHoaDon)
                .HasConstraintName("FK_Payments_Invoices");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Notifications");

            entity.ToTable("ThongBao");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.LienKetThamChieu)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.LoaiThongBao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TieuDe).HasMaxLength(255);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<TienNghi>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Amenities");

            entity.ToTable("TienNghi");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.Ten).HasMaxLength(255);
        });

        modelBuilder.Entity<TinNhanChatAi>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Chat_Messages");

            entity.ToTable("TinNhanChat_AI");

            entity.HasIndex(e => e.MaPhienChat, "IX_ChatMessages_session_id");

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.LoaiTinNhan)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("text");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NguoiGui)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MaPhienChatNavigation).WithMany(p => p.TinNhanChatAis)
                .HasForeignKey(d => d.MaPhienChat)
                .HasConstraintName("FK_ChatMessages_Sessions");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaId).HasName("PK_Roles");

            entity.ToTable("VaiTro");

            entity.HasIndex(e => e.Ten, "UQ_Roles_name").IsUnique();

            entity.Property(e => e.MaId).HasColumnName("MaID");
            entity.Property(e => e.Ten).HasMaxLength(100);

            entity.HasMany(d => d.MaQuyenHans).WithMany(p => p.MaVaiTros)
                .UsingEntity<Dictionary<string, object>>(
                    "QuyenVaiTro",
                    r => r.HasOne<QuyenHan>().WithMany()
                        .HasForeignKey("MaQuyenHan")
                        .HasConstraintName("FK_RolePermissions_Permissions"),
                    l => l.HasOne<VaiTro>().WithMany()
                        .HasForeignKey("MaVaiTro")
                        .HasConstraintName("FK_RolePermissions_Roles"),
                    j =>
                    {
                        j.HasKey("MaVaiTro", "MaQuyenHan").HasName("PK_Role_Permissions");
                        j.ToTable("QuyenVaiTro");
                    });
        });

        modelBuilder.Entity<NhatKyHeThong>(entity =>
        {
            entity.HasKey(e => e.MaId);
            entity.ToTable("NhatKyHeThong");
            entity.Property(e => e.MaId).HasColumnName("MaId");
            entity.Property(e => e.HanhDong).HasMaxLength(100);
            entity.Property(e => e.DoiTuong).HasMaxLength(100);
            entity.Property(e => e.KhoaChinh).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.DiaChiIp).HasMaxLength(50);
            entity.Property(e => e.ThoiGian).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.NhatKyHeThongs)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.MaId);
            entity.ToTable("RefreshToken");
            entity.Property(e => e.MaId).HasColumnName("MaId");
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.Property(e => e.DiaChiIpTao).HasMaxLength(50);
            entity.Property(e => e.LyDoThuHoi).HasMaxLength(255);
            entity.Property(e => e.ThayTheBoiToken).HasMaxLength(500);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
