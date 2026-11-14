using FluentAssertions;

namespace DACN.UnitTests.Common;

/// <summary>
/// Unit tests cho logic tính tienGiam trong VoucherController.KiemTra.
/// Logic được extract để test độc lập mà không cần DB.
/// </summary>
public class VoucherCalculationTests
{
    // ─── Helper: mirror logic từ VoucherController ───────────────────────────

    private static decimal TinhTienGiam(string loaiGiamGia, decimal giaTriGiamGia, decimal tongTien)
        => loaiGiamGia == "phan_tram"
            ? tongTien * giaTriGiamGia / 100
            : giaTriGiamGia;

    private static bool DatToiThieu(decimal? giaTriDatPhongToiThieu, decimal tongTien)
        => !giaTriDatPhongToiThieu.HasValue || tongTien >= giaTriDatPhongToiThieu.Value;

    // ─── Tính tienGiam ────────────────────────────────────────────────────────

    [Fact]
    public void TinhTienGiam_PhanTram_TinhDung()
    {
        // 10% của 1_000_000 = 100_000
        var tienGiam = TinhTienGiam("phan_tram", 10, 1_000_000);
        tienGiam.Should().Be(100_000);
    }

    [Fact]
    public void TinhTienGiam_PhanTram_50Percent()
    {
        var tienGiam = TinhTienGiam("phan_tram", 50, 2_000_000);
        tienGiam.Should().Be(1_000_000);
    }

    [Fact]
    public void TinhTienGiam_CoDinh_TraVeGiaTriCoDinh()
    {
        var tienGiam = TinhTienGiam("co_dinh", 200_000, 5_000_000);
        tienGiam.Should().Be(200_000);
    }

    [Fact]
    public void TinhTienGiam_CoDinh_KhongPhuThuocTongTien()
    {
        // co_dinh không phụ thuộc vào tongTien
        var tienGiam1 = TinhTienGiam("co_dinh", 100_000, 500_000);
        var tienGiam2 = TinhTienGiam("co_dinh", 100_000, 10_000_000);
        tienGiam1.Should().Be(tienGiam2);
    }

    [Fact]
    public void TinhTienGiam_UnknownLoai_TraVeGiaTriCoDinh()
    {
        // Nếu LoaiGiamGia không phải "phan_tram" → fallback = co_dinh (giá trị tuyệt đối)
        var tienGiam = TinhTienGiam("other_type", 50_000, 1_000_000);
        tienGiam.Should().Be(50_000);
    }

    // ─── Kiểm tra GiaTriDatPhongToiThieu ─────────────────────────────────────

    [Fact]
    public void DatToiThieu_ReturnsTrue_WhenNoMinimum()
    {
        DatToiThieu(null, 100_000).Should().BeTrue();
    }

    [Fact]
    public void DatToiThieu_ReturnsTrue_WhenTongTienExceedsMinimum()
    {
        DatToiThieu(500_000, 1_000_000).Should().BeTrue();
    }

    [Fact]
    public void DatToiThieu_ReturnsTrue_WhenTongTienEqualsMinimum()
    {
        DatToiThieu(500_000, 500_000).Should().BeTrue();
    }

    [Fact]
    public void DatToiThieu_ReturnsFalse_WhenTongTienBelowMinimum()
    {
        DatToiThieu(500_000, 300_000).Should().BeFalse();
    }

    [Fact]
    public void DatToiThieu_ReturnsFalse_WhenZeroTongTien_AndMinimumSet()
    {
        DatToiThieu(1_000, 0).Should().BeFalse();
    }

    // ─── Kịch bản end-to-end ─────────────────────────────────────────────────

    public static IEnumerable<object?[]> EndToEndCases =>
    [
        ["phan_tram", 15m,      1_000_000m, (decimal?)null,    150_000m],
        ["phan_tram", 20m,      2_000_000m, (decimal?)500_000m, 400_000m],
        ["co_dinh",  200_000m, 5_000_000m, (decimal?)null,    200_000m],
        ["co_dinh",   50_000m, 1_000_000m, (decimal?)500_000m,  50_000m],
    ];

    [Theory]
    [MemberData(nameof(EndToEndCases))]
    public void EndToEnd_TinhTienGiam_DungVoiNhieuTruongHop(
        string loai, decimal giaTri, decimal tongTien, decimal? toiThieu, decimal expected)
    {
        DatToiThieu(toiThieu, tongTien).Should().BeTrue("tongTien phải đạt tối thiểu");
        TinhTienGiam(loai, giaTri, tongTien).Should().Be(expected);
    }
}
