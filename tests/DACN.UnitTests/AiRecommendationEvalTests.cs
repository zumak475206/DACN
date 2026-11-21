using System.Net.Http;
using System.Text.RegularExpressions;
using DACN.Infrastructure.Data;
using DACN.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DACN.UnitTests;

public class AiRecommendationEvalTests
{
    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    private readonly IGeoService _geoService = new GeoService();

    [Fact]
    public void Test_1_RadiusConstraint_1DayTrip_FromHcm_RecommendsVungTau_RejectsDalatAndDanang()
    {
        // Khách ở TP.HCM (10.7769, 106.7009), đi trong ngày (1 ngày)
        var (hcmLat, hcmLng) = (10.7769, 106.7009);
        var maxRadius = _geoService.LayBanKinhToiDaTheoSoNgay(1); // 150 km

        // Vũng Tàu (Bãi Sau: 10.3375, 107.0910)
        var kmToVungTau = _geoService.TinhKhoangCachHaversineKm(hcmLat, hcmLng, 10.3375, 107.0910);

        // Đà Lạt (Hồ Tuyền Lâm: 11.9022, 108.4358)
        var kmToDaLat = _geoService.TinhKhoangCachHaversineKm(hcmLat, hcmLng, 11.9022, 108.4358);

        // Đà Nẵng (Mỹ Khê: 16.0594, 108.2467)
        var kmToDaNang = _geoService.TinhKhoangCachHaversineKm(hcmLat, hcmLng, 16.0594, 108.2467);

        // Khẳng định:
        Assert.Equal(150.0, maxRadius);
        Assert.True(kmToVungTau <= maxRadius, $"Vũng Tàu ({kmToVungTau}km) phải nằm trong bán kính 1 ngày ({maxRadius}km)");
        Assert.True(kmToDaLat > maxRadius, $"Đà Lạt ({kmToDaLat}km) phải nằm ngoài bán kính 1 ngày ({maxRadius}km)");
        Assert.True(kmToDaNang > maxRadius, $"Đà Nẵng ({kmToDaNang}km) phải nằm ngoài bán kính 1 ngày ({maxRadius}km)");
    }

    [Fact]
    public void Test_2_RainAvoidance_WhenRainAbove60Percent_IndoorAttractionOutranksOutdoorAttraction()
    {
        var weatherService = new WeatherService(
            new TestHttpClientFactory(),
            new MemoryCache(new MemoryCacheOptions()),
            TimeProvider.System,
            NullLogger<WeatherService>.Instance);

        var rainyWeather = new WeatherForecastInfo
        {
            XacSuatMuaPhanTram = 85,
            LuongMuaMm = 25.0,
            MoTaThoiTiet = "Mưa to, xác suất mưa 85%"
        };

        // Điểm ngoài trời (biển, núi rừng)
        var scoreOutdoor = weatherService.ChamDiemThoiTiet(rainyWeather, "bien", ngoaiTroi: true);

        // Điểm trong nhà (bảo tàng, Landmark 81, nhà tù lịch sử)
        var scoreIndoor = weatherService.ChamDiemThoiTiet(rainyWeather, "bao_tang", ngoaiTroi: false);

        // Khẳng định: Điểm trong nhà phải được ưu tiên điểm cao hơn vượt trội khi trời mưa
        Assert.True(scoreIndoor >= 0.90m, $"Điểm trong nhà ngày mưa phải >= 0.90, thực tế: {scoreIndoor}");
        Assert.True(scoreOutdoor <= 0.35m, $"Điểm ngoài trời ngày mưa phải <= 0.35, thực tế: {scoreOutdoor}");
        Assert.True(scoreIndoor > scoreOutdoor, "Khi trời mưa lớn, điểm trong nhà phải xếp hạng cao hơn điểm ngoài trời");
    }

    [Fact]
    public void Test_3_IslandRule_PhuQuocRequiresFlightOrFerry_WithAppropriateBudget()
    {
        // Khoảng cách chim bay từ HCM đến Phú Quốc ~300km
        var haversineKm = 300.0;
        var (kmDuongBo, thoiGianPhut, chiPhiKhuHoi, phuongTien) = _geoService.UocTinhDiChuyen(
            haversineKm, laDeoNui: false, laBienDao: true, vungXuatPhat: "south");

        // Khẳng định: Biển đảo phải có phương tiện máy bay/tàu cao tốc và chi phí vé riêng
        Assert.True(chiPhiKhuHoi >= 1_500_000m, $"Vé ra đảo khứ hồi phải >= 1.5 triệu, thực tế: {chiPhiKhuHoi:N0}");
        Assert.Contains("Máy bay", phuongTien);
    }

    [Fact]
    public async Task Test_4_TimeThreshold_Beyond16Days_UsesSeasonalClimateFallback()
    {
        var weatherService = new WeatherService(
            new TestHttpClientFactory(),
            new MemoryCache(new MemoryCacheOptions()),
            TimeProvider.System,
            NullLogger<WeatherService>.Instance);

        // Ngày đi: 45 ngày nữa tính từ hôm nay (> 16 ngày)
        var ngayDiXa = DateTime.Today.AddDays(45);

        // Đà Lạt (11.9028, 108.4341)
        var weather = await weatherService.LayDuBaoThoiTietAsync(11.9028, 108.4341, ngayDiXa);

        // Khẳng định: Phải dùng nguồn ước lượng theo mùa và có thông tin mùa
        Assert.Contains("khí hậu", weather.NguonDuLieu);
        Assert.False(string.IsNullOrWhiteSpace(weather.MoTaThoiTiet));
    }

    [Fact]
    public void Test_5_NoDigitsClaimVerifier_CatchesAnyDigitInLlmText()
    {
        var rawLlmTextWithDigits = "Nơi này cách trung tâm 25km và giá vé chỉ 50.000đ rất rẻ vào ngày 12 tháng 12.";
        var cleanQualitativeText = "Không gian thoáng đãng, nhiều góc check-in thơ mộng phù hợp để chụp ảnh sống ảo và ngắm hoàng hôn.";

        // Quy tắc No-Digits Claim Verifier
        var hasDigitsInBadText = Regex.IsMatch(rawLlmTextWithDigits, @"\d");
        var hasDigitsInCleanText = Regex.IsMatch(cleanQualitativeText, @"\d");

        Assert.True(hasDigitsInBadText, "Verifier phải bắt được chữ số trong câu văn của LLM");
        Assert.False(hasDigitsInCleanText, "Câu văn định tính chuẩn không được chứa chữ số");
    }

    [Fact]
    public void Test_6_GPSPrivacy_CoordinatesAreRoundedTo2Decimals()
    {
        double exactLat = 10.77688912;
        double exactLng = 106.70089234;

        var (roundedLat, roundedLng) = _geoService.LamTronToaDo(exactLat, exactLng);

        Assert.Equal(10.78, roundedLat);
        Assert.Equal(106.70, roundedLng);
    }
}
