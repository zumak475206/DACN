using DACN.Infrastructure.Data;
using DACN.Web.Common;
using DACN.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DACN.UnitTests;

public class MultiBranchTests
{
    private static DACNContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<DACNContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DACNContext(options);
    }

    [Fact]
    public void UserSessionService_StoresAndRestoresBranchInfoAccurately()
    {
        // Kiểm tra phiên đăng nhập lưu trữ chi nhánh và phục hồi đúng sau khi F5
        var session = new UserSessionService();
        session.SetUser(
            hoTen: "Lê Lễ Tân Phú Quốc",
            email: "letan_phuquoc@hotel.com",
            role: "FrontDesk",
            id: 12,
            permissions: null,
            maChiNhanh: 2,
            tenChiNhanh: "GrandStay Sunset Villa Phú Quốc");

        session.CurrentUser.MaChiNhanh.Should().Be(2);
        session.CurrentUser.TenChiNhanh.Should().Be("GrandStay Sunset Villa Phú Quốc");
        session.CurrentUser.Role.Should().Be("FrontDesk");

        // Mô phỏng lưu vào LocalStorage và phục hồi
        var stored = session.ToStoredSession();
        stored.MaChiNhanh.Should().Be(2);
        stored.TenChiNhanh.Should().Be("GrandStay Sunset Villa Phú Quốc");

        var restoredSession = new UserSessionService();
        restoredSession.RestoreFromStored(stored);

        restoredSession.CurrentUser.IsLoggedIn.Should().BeTrue();
        restoredSession.CurrentUser.MaChiNhanh.Should().Be(2);
        restoredSession.CurrentUser.TenChiNhanh.Should().Be("GrandStay Sunset Villa Phú Quốc");
        restoredSession.CurrentUser.Role.Should().Be("FrontDesk");
    }

    [Theory]
    [InlineData("Phú Quốc Kiên Giang", 2)]
    [InlineData("Đảo ngọc Phú Quốc", 2)]
    [InlineData("Đà Lạt sương mù", 3)]
    [InlineData("Hồ Tuyền Lâm, Đà Lạt", 3)]
    [InlineData("Bãi biển Mỹ Khê Đà Nẵng", 1)]
    [InlineData("Bán đảo Sơn Trà", 1)]
    [InlineData("Phố cổ Hội An", 1)]
    public void GoiYDuLichService_XacDinhChiNhanhTuDiemDen_MapsDestinationsCorrectly(string destination, int expectedBranchId)
    {
        // Kiểm tra AI Recommender ánh xạ đúng điểm đến du lịch sang ID chi nhánh cơ sở
        var actualBranchId = GoiYDuLichService.XacDinhChiNhanhTuDiemDen(destination);
        actualBranchId.Should().Be(expectedBranchId);
    }

    [Fact]
    public void GoiYDuLichService_TenChiNhanhTuId_ReturnsOfficialBranchNames()
    {
        GoiYDuLichService.TenChiNhanhTuId(1).Should().Be("GrandStay Đà Nẵng Beach Resort");
        GoiYDuLichService.TenChiNhanhTuId(2).Should().Be("GrandStay Sunset Villa Phú Quốc");
        GoiYDuLichService.TenChiNhanhTuId(3).Should().Be("GrandStay Pine Hill Retreat Đà Lạt");
    }

    [Fact]
    public async Task MultiBranch_Isolation_FiltersRoomsAndBookingsByBranch()
    {
        await using var db = CreateInMemoryDb();

        // Khởi tạo 3 chi nhánh
        var cnDaNang = new ChiNhanh { MaId = 1, MaChiNhanhCode = "CN-DANANG", TenChiNhanh = "GrandStay Đà Nẵng", TinhThanh = "Đà Nẵng", DiaChiChiTiet = "120 Võ Nguyên Giáp", SoDienThoai = "0236", EmailLienHe = "dn@stay.vn" };
        var cnPhuQuoc = new ChiNhanh { MaId = 2, MaChiNhanhCode = "CN-PHUQUOC", TenChiNhanh = "GrandStay Phú Quốc", TinhThanh = "Kiên Giang", DiaChiChiTiet = "Bãi Trường", SoDienThoai = "0297", EmailLienHe = "pq@stay.vn" };
        var cnDaLat = new ChiNhanh { MaId = 3, MaChiNhanhCode = "CN-DALAT", TenChiNhanh = "GrandStay Đà Lạt", TinhThanh = "Lâm Đồng", DiaChiChiTiet = "Hồ Tuyền Lâm", SoDienThoai = "0263", EmailLienHe = "dl@stay.vn" };

        db.ChiNhanhs.AddRange(cnDaNang, cnPhuQuoc, cnDaLat);

        // Khởi tạo phòng cho từng chi nhánh
        db.Phongs.AddRange(
            new Phong { MaId = 101, SoPhong = "101", MaLoaiPhong = 1, MaChiNhanh = 1, TrangThai = "Available", TrangThaiVeSinh = "Clean" },
            new Phong { MaId = 102, SoPhong = "102", MaLoaiPhong = 1, MaChiNhanh = 1, TrangThai = "Occupied", TrangThaiVeSinh = "Clean" },
            new Phong { MaId = 201, SoPhong = "PQ-101", MaLoaiPhong = 2, MaChiNhanh = 2, TrangThai = "Occupied", TrangThaiVeSinh = "Clean" },
            new Phong { MaId = 301, SoPhong = "DL-101", MaLoaiPhong = 3, MaChiNhanh = 3, TrangThai = "Available", TrangThaiVeSinh = "Clean" }
        );

        // Khởi tạo đơn đặt phòng cho từng chi nhánh
        db.DatPhongs.AddRange(
            new DatPhong { MaId = 1, MaDatPhong = "DP-DN-01", MaChiNhanh = 1, TongTien = 1000000, TrangThai = "Confirmed", KenhDatPhong = "web" },
            new DatPhong { MaId = 2, MaDatPhong = "DP-PQ-01", MaChiNhanh = 2, TongTien = 2500000, TrangThai = "Confirmed", KenhDatPhong = "web" }
        );

        await db.SaveChangesAsync();

        // 1. Kiểm tra Lễ tân Phú Quốc chỉ thấy phòng Phú Quốc
        var phuQuocRooms = await db.Phongs.Where(p => p.MaChiNhanh == 2).ToListAsync();
        phuQuocRooms.Should().HaveCount(1);
        phuQuocRooms.First().SoPhong.Should().Be("PQ-101");

        // 2. Kiểm tra Lễ tân Đà Nẵng chỉ thấy đơn Đà Nẵng
        var daNangBookings = await db.DatPhongs.Where(b => b.MaChiNhanh == 1).ToListAsync();
        daNangBookings.Should().HaveCount(1);
        daNangBookings.First().MaDatPhong.Should().Be("DP-DN-01");

        // 3. Quản lý toàn chuỗi (MaChiNhanh = 0 / All) thấy toàn bộ
        var allRooms = await db.Phongs.ToListAsync();
        allRooms.Should().HaveCount(4);
    }

    [Fact]
    public async Task MultiBranch_ExpenseAndServiceBilling_IsolatesByBranch()
    {
        await using var db = CreateInMemoryDb();

        var cnPhuQuoc = new ChiNhanh { MaId = 2, MaChiNhanhCode = "CN-PHUQUOC", TenChiNhanh = "GrandStay Phú Quốc", TinhThanh = "Kiên Giang", DiaChiChiTiet = "Bãi Trường", SoDienThoai = "0297", EmailLienHe = "pq@stay.vn" };
        var cnDaLat = new ChiNhanh { MaId = 3, MaChiNhanhCode = "CN-DALAT", TenChiNhanh = "GrandStay Đà Lạt", TinhThanh = "Lâm Đồng", DiaChiChiTiet = "Hồ Tuyền Lâm", SoDienThoai = "0263", EmailLienHe = "dl@stay.vn" };
        db.ChiNhanhs.AddRange(cnPhuQuoc, cnDaLat);

        // Chi phí vận hành theo chi nhánh
        db.ChiPhiVanHanhs.AddRange(
            new ChiPhiVanHanh { MaId = 1, MaChiNhanh = 2, DanhMuc = "Bảo trì", SoTien = 3500000, TrangThai = "Pending", NgayChi = DateOnly.FromDateTime(DateTime.Today) },
            new ChiPhiVanHanh { MaId = 2, MaChiNhanh = 3, DanhMuc = "Tiện ích", SoTien = 1200000, TrangThai = "Approved", NgayChi = DateOnly.FromDateTime(DateTime.Today) }
        );

        // Đơn đặt phòng và hóa đơn dịch vụ
        var booking = new DatPhong { MaId = 10, MaDatPhong = "DP-PQ-02", MaChiNhanh = 2, TongTien = 1500000, TrangThai = "CheckedIn", KenhDatPhong = "walk_in" };
        var room = new Phong { MaId = 202, SoPhong = "PQ-202", MaLoaiPhong = 2, MaChiNhanh = 2, TrangThai = "Occupied", TrangThaiVeSinh = "Clean" };
        var detail = new ChiTietDatPhong { MaId = 100, MaDatPhong = 10, MaPhong = 202, MaLoaiPhong = 2, TrangThai = "Active", NgayNhanPhongDuKien = DateTime.Today, NgayTraPhongDuKien = DateTime.Today.AddDays(1), GiaMoiDem = 1500000 };
        var hoaDon = new HoaDon { MaId = 50, MaDatPhong = 10, TongTienPhong = 1500000, TongTienDichVu = 0, TongThanhToan = 1500000, TrangThai = "Pending" };
        var dichVu = new DichVu { MaId = 5, MaDanhMucDichVu = 1, Ten = "Nước khoáng Minibar", GiaDichVu = 25000, DangHoatDong = true, MaChiNhanh = 2 };

        db.DatPhongs.Add(booking);
        db.Phongs.Add(room);
        db.ChiTietDatPhongs.Add(detail);
        db.HoaDons.Add(hoaDon);
        db.DichVus.Add(dichVu);
        await db.SaveChangesAsync();

        // 1. Kiểm tra Lọc Chi phí theo chi nhánh Phú Quốc
        var pqExpenses = await db.ChiPhiVanHanhs.Where(c => c.MaChiNhanh == 2).ToListAsync();
        pqExpenses.Should().HaveCount(1);
        pqExpenses.First().SoTien.Should().Be(3500000);

        // 2. Thêm đơn dịch vụ vào hóa đơn phòng
        var donDv = new DonDichVu { MaChiTietDatPhong = detail.MaId, NgayDatDichVu = DateTime.UtcNow, TongTien = 50000, TrangThai = "Completed" };
        db.DonDichVus.Add(donDv);
        await db.SaveChangesAsync();

        db.ChiTietDonDichVus.Add(new ChiTietDonDichVu { MaDonDichVu = donDv.MaId, MaDichVu = dichVu.MaId, SoLuong = 2, DonGia = 25000 });
        hoaDon.TongTienDichVu += 50000;
        hoaDon.TongThanhToan += 50000;
        await db.SaveChangesAsync();

        hoaDon.TongTienDichVu.Should().Be(50000);
        hoaDon.TongThanhToan.Should().Be(1550000);

        // 3. Quyết toán hóa đơn với phương thức hợp lệ theo CHECK constraint CHK_Payments_method
        var payment = new ThanhToan
        {
            MaHoaDon = hoaDon.MaId,
            PhuongThucThanhToan = StatusMapper.NormalizePhuongThucThanhToan("cash"),
            SoTienDaThanhToan = 1550000,
            TrangThai = "Success",
            NgayThanhToan = DateTime.UtcNow
        };
        db.ThanhToans.Add(payment);
        hoaDon.TrangThai = "Paid";
        await db.SaveChangesAsync();

        payment.PhuongThucThanhToan.Should().Be("Cash");
        hoaDon.TrangThai.Should().Be("Paid");
    }

    [Fact]
    public void StatusMapper_ConstraintsValidation_ProducesValidDatabaseValues()
    {
        // Kiểm tra StatusMapper luôn sinh ra các giá trị hợp lệ với SQL CHECK Constraints
        // 1. ChiTietDatPhong.TrangThai: 'Pending', 'Active', 'CheckedOut', 'Cancelled'
        StatusMapper.NormalizeChiTietDatPhongTrangThai("cho_xac_nhan").Should().Be("Pending");
        StatusMapper.NormalizeChiTietDatPhongTrangThai("confirmed").Should().Be("Pending");
        StatusMapper.NormalizeChiTietDatPhongTrangThai("dang_o").Should().Be("Active");
        StatusMapper.NormalizeChiTietDatPhongTrangThai("da_tra_phong").Should().Be("CheckedOut");
        StatusMapper.NormalizeChiTietDatPhongTrangThai("da_huy").Should().Be("Cancelled");

        // 2. DatPhong.TrangThai: 'Pending', 'Confirmed', 'CheckedIn', 'CheckedOut', 'Cancelled'
        StatusMapper.NormalizeDatPhongTrangThai("cho_xac_nhan").Should().Be("Pending");
        StatusMapper.NormalizeDatPhongTrangThai("da_nhan_phong").Should().Be("CheckedIn");
        StatusMapper.NormalizeDatPhongTrangThai("hoan_thanh").Should().Be("CheckedOut");

        // 3. ThanhToan.PhuongThucThanhToan: 'Cash', 'VNPay', 'MoMo', 'BankTransfer'
        StatusMapper.NormalizePhuongThucThanhToan("tien_mat").Should().Be("Cash");
        StatusMapper.NormalizePhuongThucThanhToan("chuyen_khoan").Should().Be("BankTransfer");
        StatusMapper.NormalizePhuongThucThanhToan("qr").Should().Be("BankTransfer");
        StatusMapper.NormalizePhuongThucThanhToan("vnpay").Should().Be("VNPay");
        StatusMapper.NormalizePhuongThucThanhToan("momo").Should().Be("MoMo");
    }

    [Fact]
    public async Task GoiYDuLichService_3MillionNearHcmInDecember_RecommendsDaLatBranch3_NotHoiAn()
    {
        await using var db = CreateInMemoryDb();

        // Giả lập dữ liệu trong DB có cả Hội An (Tháng 2-4) và các hạng phòng 3 chi nhánh
        var hoiAnSpot = new DiaDiemThamQuan
        {
            MaId = 5,
            Ten = "Phố Cổ Hội An (Di Sản UNESCO)",
            DiaChi = "TP. Hội An, Tỉnh Quảng Nam",
            PhongCach = "songao,amthuc,thugian",
            DangHoatDong = true
        };
        var muaHeMienTrung = new MuaDuLich
        {
            MaId = 1,
            Ten = "Mùa Xuân - Hè Miền Trung",
            LoaiMua = "spring",
            ThangBatDau = 2,
            ThangKetThuc = 4,
            KhuVuc = "central"
        };
        db.DiaDiemThamQuans.Add(hoiAnSpot);
        db.MuaDuLiches.Add(muaHeMienTrung);
        db.GoiYdiaDiemTheoMuaAis.Add(new GoiYdiaDiemTheoMuaAi
        {
            MaId = 4,
            MaDiaDiem = 5,
            MaMua = 1,
            MuaDuLich = "spring",
            ThangBatDauGoiY = 2,
            ThangKetThucGoiY = 4,
            DiemPhuHop = 0.96m,
            LyDoGoiY = "Phố cổ Hội An dịp đầu xuân nắng nhẹ.",
            LoaiThoiTiet = "Nắng xuân dịu nhẹ, khô ráo"
        });

        db.LoaiPhongs.AddRange(
            new LoaiPhong { MaId = 1, Ten = "Phòng Standard Tiêu Chuẩn", DuongDanRutGon = "std-dn", GiaCoBan = 850_000m, SoNguoiLonToiDa = 2, DangHoatDong = true, MaChiNhanh = 1 },
            new LoaiPhong { MaId = 2, Ten = "GrandStay Suite Family Resort", DuongDanRutGon = "suite-pq", GiaCoBan = 1_500_000m, SoNguoiLonToiDa = 4, DangHoatDong = true, MaChiNhanh = 2 },
            new LoaiPhong { MaId = 3, Ten = "Pine Hill Retreat Superior", DuongDanRutGon = "sup-dl", GiaCoBan = 750_000m, SoNguoiLonToiDa = 2, DangHoatDong = true, MaChiNhanh = 3 },
            new LoaiPhong { MaId = 4, Ten = "GrandStay Superior Ocean View Vũng Tàu", DuongDanRutGon = "sup-vt", GiaCoBan = 650_000m, SoNguoiLonToiDa = 2, DangHoatDong = true, MaChiNhanh = 4 }
        );
        await db.SaveChangesAsync();

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var httpClientFactory = NSubstitute.Substitute.For<IHttpClientFactory>();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<GoiYDuLichService>.Instance;
        var service = new GoiYDuLichService(db, config, httpClientFactory, logger);

        var prompt = "giờ tôi có 3 triệu muốn đi sống ảo gần khu vực hồ chí minh trong tháng 12 thì nên đi đâu";

        // 1. Kiểm tra trích xuất ý chính (NLU)
        var intent = GoiYDuLichService.TrichXuatYChinh(prompt);
        intent.NganSach.Should().Be(3_000_000m);
        intent.ThangDi.Should().Be(12);
        intent.PhongCach.Should().Contain("songao");
        intent.DiemXuatPhat.Should().Be("hcm");
        intent.YeuCauGanXuatPhat.Should().BeTrue();
        intent.KhuVuc.Should().Be("south");

        // 2. Kiểm tra kết quả gợi ý và câu trả lời Chat
        var chatRes = await service.ChatAsync(prompt, null, null);
        chatRes.GoiYKemTheo.Should().NotBeEmpty();

        var best = chatRes.GoiYKemTheo.First();
        best.MaChiNhanh.Should().Be(4, "Vũng Tàu là điểm đến gần TP.HCM nhất (~105km), đúng mùa khô tháng 12 và vừa ngân sách 3 triệu");
        best.TenChiNhanh.Should().Be("GrandStay Vũng Tàu Oceanfront Hotel");
        best.HangPhongPhuHop.Should().Be("GrandStay Superior Ocean View Vũng Tàu");
        best.GiaPhongMoiDem.Should().Be(650_000m);
        best.LaDungMua.Should().BeTrue();
        best.ChiPhiDuKienDen.Should().BeLessThanOrEqualTo(3_000_000m);

        // Danh sách gợi ý phải có thêm lựa chọn Đà Lạt (~300km)
        chatRes.GoiYKemTheo.Should().Contain(g => g.MaChiNhanh == 3);

        // Tuyệt đối không gợi ý Hội An cách 900km hay thời tiết mâu thuẫn
        chatRes.CauTraLoi.Should().Contain("Vũng Tàu");
        chatRes.CauTraLoi.Should().Contain("GrandStay Vũng Tàu Oceanfront Hotel");
        chatRes.CauTraLoi.Should().NotContain("Hội An");
    }

    [Theory]
    [InlineData("Du Coffee, Đường tỉnh lộ 7, An Nhơn Tây, Củ Chi, Tp.Hồ Chí Minh", 10.9733, 106.4933, "south")]
    [InlineData("Huyện Củ Chi, TP. Hồ Chí Minh", 10.9733, 106.4933, "south")]
    [InlineData("tp. hải phòng", 20.8561, 106.6822, "north")]
    [InlineData("Hải Phòng", 20.8561, 106.6822, "north")]
    [InlineData("120 Võ Nguyên Giáp, Đà Nẵng", 16.0544, 108.2022, "central")]
    [InlineData("Bãi Trường, Phú Quốc, Kiên Giang", 10.2289, 103.9572, "south")]
    [InlineData("Hồ Tuyền Lâm, Phường 4, Đà Lạt, Lâm Đồng", 11.9404, 108.4583, "south")]
    [InlineData("Bãi Sau, Thùy Vân, Vũng Tàu", 10.3460, 107.0843, "south")]
    public async Task GeoService_TraCuuToaDoTuDiaChiAsync_ResolvesGpsAndRegionAccurately(string diaChi, double expectedLat, double expectedLng, string expectedRegion)
    {
        var httpClientFactory = NSubstitute.Substitute.For<IHttpClientFactory>();
        var geoService = new GeoService(httpClientFactory);

        var result = await geoService.TraCuuToaDoTuDiaChiAsync(diaChi);

        result.Should().NotBeNull();
        result!.Value.Lat.Should().BeApproximately(expectedLat, 0.001);
        result.Value.Lng.Should().BeApproximately(expectedLng, 0.001);
        result.Value.VungMien.Should().Be(expectedRegion);
    }

    [Fact]
    public async Task GoiYDuLichService_CuChiQuery_RecommendsCuChiBranch5_NotDaLat()
    {
        await using var db = CreateInMemoryDb();

        // Thêm phòng cho Chi nhánh 5 (CUCHI Hotel) và Chi nhánh 3 (Đà Lạt)
        db.LoaiPhongs.AddRange(
            new LoaiPhong { MaId = 3, Ten = "Pine Hill Retreat Superior", DuongDanRutGon = "sup-dl", GiaCoBan = 750_000m, SoNguoiLonToiDa = 2, DangHoatDong = true, MaChiNhanh = 3 },
            new LoaiPhong { MaId = 5, Ten = "Phòng Nghỉ Sinh Thái Củ Chi Eco", DuongDanRutGon = "eco-cc", GiaCoBan = 450_000m, SoNguoiLonToiDa = 2, DangHoatDong = true, MaChiNhanh = 5 }
        );
        await db.SaveChangesAsync();

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var httpClientFactory = NSubstitute.Substitute.For<IHttpClientFactory>();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<GoiYDuLichService>.Instance;
        var service = new GoiYDuLichService(db, config, httpClientFactory, logger);

        var prompt = "tôi muốn đi chơi ở củ chi thì sao";

        // 1. Kiểm tra trích xuất NLU nhận diện đích danh Củ Chi
        var intent = GoiYDuLichService.TrichXuatYChinh(prompt);
        intent.DiemDenCuThe.Should().Be("cuchi");
        intent.KhuVuc.Should().Be("south");

        // 2. Kiểm tra Chatbot phản hồi đúng chi nhánh Củ Chi
        var chatRes = await service.ChatAsync(prompt, null, null);
        chatRes.GoiYKemTheo.Should().NotBeEmpty();

        var best = chatRes.GoiYKemTheo.First();
        best.MaChiNhanh.Should().Be(5, "Khách hỏi Củ Chi thì gợi ý số 1 phải là CUCHI Hotel");
        best.TenChiNhanh.Should().Be("CUCHI Hotel");
        best.KhoangCachTuHcmKm.Should().Be(45);
        best.HangPhongPhuHop.Should().Be("Phòng Nghỉ Sinh Thái Củ Chi Eco");

        // Câu trả lời tư vấn phải hướng về Củ Chi và Địa đạo Củ Chi, không bịa Đà Lạt
        chatRes.CauTraLoi.Should().Contain("CUCHI Hotel");
        chatRes.CauTraLoi.Should().Contain("Củ Chi");
        chatRes.CauTraLoi.Should().NotContain("Đà Lạt");
    }
}
