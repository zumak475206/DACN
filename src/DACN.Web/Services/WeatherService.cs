using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace DACN.Web.Services;

public class WeatherForecastInfo
{
    public double NhietDoMax { get; set; } = 28.0;
    public double NhietDoMin { get; set; } = 22.0;
    public int XacSuatMuaPhanTram { get; set; } = 15;
    public double LuongMuaMm { get; set; } = 0.0;
    public double TocDoGioKmH { get; set; } = 12.0;
    public double ChiSoUv { get; set; } = 5.0;
    public int MaThoiTietCode { get; set; } = 0;
    public string MoTaThoiTiet { get; set; } = "Nắng đẹp, khô ráo";
    public bool CanhBaoNguyHiem { get; set; } = false;
    public string NguonDuLieu { get; set; } = "Dự báo thực tế Open-Meteo";
}

public interface IWeatherService
{
    Task<WeatherForecastInfo> LayDuBaoThoiTietAsync(double lat, double lng, DateTime ngayDi, CancellationToken ct = default);
    decimal ChamDiemThoiTiet(WeatherForecastInfo weather, string? theLoai, bool ngoaiTroi);
}

public class WeatherService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    TimeProvider timeProvider,
    ILogger<WeatherService> logger) : IWeatherService
{
    public async Task<WeatherForecastInfo> LayDuBaoThoiTietAsync(double lat, double lng, DateTime ngayDi, CancellationToken ct = default)
    {
        var homNay = timeProvider.GetLocalNow().Date;
        var soNgay = (ngayDi.Date - homNay).Days;

        // Nếu ngày đi trong phạm vi 16 ngày dự báo thật (Open-Meteo 16-day forecast)
        if (soNgay >= 0 && soNgay <= 16)
        {
            var cacheKey = $"weather_{Math.Round(lat, 2):F2}_{Math.Round(lng, 2):F2}_{ngayDi:yyyyMMdd}";
            if (cache.TryGetValue(cacheKey, out WeatherForecastInfo? cachedWeather) && cachedWeather != null)
            {
                return cachedWeather;
            }

            try
            {
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}&longitude={lng.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max,wind_speed_10m_max,uv_index_max&timezone=Asia%2FHo_Chi_Minh";

                var response = await client.GetFromJsonAsync<OpenMeteoResponse>(url, ct);
                if (response?.Daily?.Time != null)
                {
                    var targetDateStr = ngayDi.ToString("yyyy-MM-dd");
                    var idx = response.Daily.Time.IndexOf(targetDateStr);
                    if (idx < 0) idx = 0; // Nếu không khớp chính xác, lấy ngày gần nhất

                    var weatherCode = response.Daily.WeatherCode != null && response.Daily.WeatherCode.Count > idx ? response.Daily.WeatherCode[idx] : 0;
                    var tempMax = response.Daily.TemperatureMax != null && response.Daily.TemperatureMax.Count > idx ? response.Daily.TemperatureMax[idx] : 28.0;
                    var tempMin = response.Daily.TemperatureMin != null && response.Daily.TemperatureMin.Count > idx ? response.Daily.TemperatureMin[idx] : 22.0;
                    var precipSum = response.Daily.PrecipitationSum != null && response.Daily.PrecipitationSum.Count > idx ? response.Daily.PrecipitationSum[idx] : 0.0;
                    var precipProb = response.Daily.PrecipitationProbabilityMax != null && response.Daily.PrecipitationProbabilityMax.Count > idx ? response.Daily.PrecipitationProbabilityMax[idx] : 10;
                    var windSpeed = response.Daily.WindSpeedMax != null && response.Daily.WindSpeedMax.Count > idx ? response.Daily.WindSpeedMax[idx] : 10.0;
                    var uv = response.Daily.UvIndexMax != null && response.Daily.UvIndexMax.Count > idx ? response.Daily.UvIndexMax[idx] : 5.0;

                    var isDangerous = precipSum >= 50.0 || windSpeed >= 60.0 || weatherCode >= 95;

                    var result = new WeatherForecastInfo
                    {
                        NhietDoMax = Math.Round(tempMax, 1),
                        NhietDoMin = Math.Round(tempMin, 1),
                        XacSuatMuaPhanTram = precipProb,
                        LuongMuaMm = Math.Round(precipSum, 1),
                        TocDoGioKmH = Math.Round(windSpeed, 1),
                        ChiSoUv = Math.Round(uv, 1),
                        MaThoiTietCode = weatherCode,
                        CanhBaoNguyHiem = isDangerous,
                        MoTaThoiTiet = GiaiMaWeatherCode(weatherCode, tempMin, tempMax, precipProb),
                        NguonDuLieu = "Dự báo thời tiết thực tế (Open-Meteo)"
                    };

                    cache.Set(cacheKey, result, TimeSpan.FromHours(2));
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi khi gọi Open-Meteo API cho tọa độ ({Lat}, {Lng}). Chuyển sang fallback khí hậu theo mùa.", lat, lng);
            }
        }

        // Nhánh 2: Đi xa hơn 16 ngày hoặc mất kết nối mạng -> Fallback khí hậu theo mùa
        return LayUocLuongKhiHauTheoMua(lat, lng, ngayDi.Month);
    }

    public decimal ChamDiemThoiTiet(WeatherForecastInfo weather, string? theLoai, bool ngoaiTroi)
    {
        // 1. Cảnh báo nguy hiểm (bão, ngập lụt, sấm sét)
        if (weather.CanhBaoNguyHiem)
        {
            return ngoaiTroi ? 0.10m : 0.70m;
        }

        // 2. Xét mưa
        if (weather.XacSuatMuaPhanTram >= 60 || weather.LuongMuaMm >= 15.0)
        {
            // Điểm trong nhà (Bảo tàng, Thủy cung, Landmark, Di tích kín) là cứu tinh khi trời mưa!
            if (!ngoaiTroi)
            {
                return 0.95m; // Ưu tiên hàng đầu khi trời mưa
            }

            // Điểm ngoài trời (đặc biệt là biển, đèo núi, dã ngoại) bị trừ điểm nặng
            if (string.Equals(theLoai, "bien", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(theLoai, "nui_rung", StringComparison.OrdinalIgnoreCase))
            {
                return 0.25m;
            }

            return 0.35m;
        }

        // 3. Thời tiết nắng ráo, mát mẻ (Mưa thấp < 30%)
        if (weather.XacSuatMuaPhanTram <= 30)
        {
            // Nếu ngoài trời: thời tiết lý tưởng để sống ảo
            if (ngoaiTroi)
            {
                // Nhiệt độ dễ chịu 18 - 30 độ
                if (weather.NhietDoMax <= 32 && weather.NhietDoMin >= 15)
                    return 0.98m;
                return 0.90m;
            }

            return 0.88m;
        }

        // 4. Mưa nhẹ trung bình (30% - 60%)
        return ngoaiTroi ? 0.65m : 0.88m;
    }

    private static WeatherForecastInfo LayUocLuongKhiHauTheoMua(double lat, double lng, int thang)
    {
        // Phân vùng theo vĩ độ:
        // Lat >= 19.5: Miền Bắc (Hà Nội, Sa Pa)
        // Lat >= 14.5: Miền Trung (Đà Nẵng, Hội An, Huế)
        // Lat < 14.5: Miền Nam & Tây Nguyên (Đà Lạt, TP.HCM, Vũng Tàu, Phú Quốc)

        if (lat >= 19.5) // Miền Bắc
        {
            if (thang >= 11 || thang <= 2)
            {
                return new WeatherForecastInfo
                {
                    NhietDoMax = 20.0,
                    NhietDoMin = 13.0,
                    XacSuatMuaPhanTram = 20,
                    MoTaThoiTiet = "Mùa đông miền Bắc se lạnh, khô ráo",
                    NguonDuLieu = "Ước lượng theo khí hậu mùa"
                };
            }
            if (thang >= 6 && thang <= 8)
            {
                return new WeatherForecastInfo
                {
                    NhietDoMax = 35.0,
                    NhietDoMin = 27.0,
                    XacSuatMuaPhanTram = 65,
                    MoTaThoiTiet = "Mùa hè nắng nóng, thỉnh thoảng có mưa rào lớn",
                    NguonDuLieu = "Ước lượng theo khí hậu mùa"
                };
            }
            return new WeatherForecastInfo
            {
                NhietDoMax = 27.0,
                NhietDoMin = 21.0,
                XacSuatMuaPhanTram = 30,
                MoTaThoiTiet = "Tiết trời mát mẻ, nắng nhẹ",
                NguonDuLieu = "Ước lượng theo khí hậu mùa"
            };
        }

        if (lat >= 14.5) // Miền Trung (Đà Nẵng / Hội An)
        {
            if (thang >= 9 && thang <= 12)
            {
                // Mùa mưa bão miền Trung
                return new WeatherForecastInfo
                {
                    NhietDoMax = 25.0,
                    NhietDoMin = 20.0,
                    XacSuatMuaPhanTram = 75,
                    LuongMuaMm = 25.0,
                    MoTaThoiTiet = "Mùa mưa miền Trung, biển động và ẩm ướt",
                    NguonDuLieu = "Ước lượng theo khí hậu mùa"
                };
            }
            return new WeatherForecastInfo
            {
                NhietDoMax = 31.0,
                NhietDoMin = 24.0,
                XacSuatMuaPhanTram = 15,
                MoTaThoiTiet = "Mùa khô duyên hải miền Trung, nắng vàng rực rỡ, biển êm",
                NguonDuLieu = "Ước lượng theo khí hậu mùa"
            };
        }

        // Miền Nam & Tây Nguyên
        // Đà Lạt (vĩ độ ~11.9, cao nguyên mát lạnh quanh năm)
        if (lat >= 11.8 && lat <= 12.1 && lng >= 108.3 && lng <= 108.6)
        {
            if (thang >= 10 || thang <= 4)
            {
                return new WeatherForecastInfo
                {
                    NhietDoMax = 22.0,
                    NhietDoMin = 13.0,
                    XacSuatMuaPhanTram = 10,
                    MoTaThoiTiet = "Mùa khô vùng cao Đà Lạt se lạnh, sương sớm mờ ảo, nắng vàng khô ráo lý tưởng săn mây",
                    NguonDuLieu = "Ước lượng theo khí hậu mùa"
                };
            }
            return new WeatherForecastInfo
            {
                NhietDoMax = 21.0,
                NhietDoMin = 15.0,
                XacSuatMuaPhanTram = 70,
                LuongMuaMm = 20.0,
                MoTaThoiTiet = "Mùa mưa cao nguyên, sương mù dày, đường trơn",
                NguonDuLieu = "Ước lượng theo khí hậu mùa"
            };
        }

        // Phú Quốc (Đảo ngọc Tây Nam)
        if (lng <= 104.2)
        {
            if (thang >= 11 || thang <= 4)
            {
                return new WeatherForecastInfo
                {
                    NhietDoMax = 30.0,
                    NhietDoMin = 24.0,
                    XacSuatMuaPhanTram = 15,
                    MoTaThoiTiet = "Mùa khô đẹp nhất Phú Quốc: nắng vàng, biển êm trong vắt, hoàng hôn rực rỡ",
                    NguonDuLieu = "Ước lượng theo khí hậu mùa"
                };
            }
            return new WeatherForecastInfo
            {
                NhietDoMax = 29.0,
                NhietDoMin = 25.0,
                XacSuatMuaPhanTram = 70,
                LuongMuaMm = 25.0,
                MoTaThoiTiet = "Mùa mưa Tây Nam, sóng biển lớn, hạn chế tắm biển",
                NguonDuLieu = "Ước lượng theo khí hậu mùa"
            };
        }

        // TP.HCM & Vũng Tàu (Đông Nam Bộ)
        if (thang >= 11 || thang <= 4)
        {
            return new WeatherForecastInfo
            {
                NhietDoMax = 31.0,
                NhietDoMin = 23.0,
                XacSuatMuaPhanTram = 10,
                MoTaThoiTiet = "Mùa khô phương Nam nắng ráo, gió biển mát lành, biển êm",
                NguonDuLieu = "Ước lượng theo khí hậu mùa"
            };
        }

        return new WeatherForecastInfo
        {
            NhietDoMax = 32.0,
            NhietDoMin = 25.0,
            XacSuatMuaPhanTram = 60,
            MoTaThoiTiet = "Mùa mưa phương Nam, thường có mưa rào chiều tối",
            NguonDuLieu = "Ước lượng theo khí hậu mùa"
        };
    }

    private static string GiaiMaWeatherCode(int code, double minTemp, double maxTemp, int precipProb)
    {
        var tempStr = $"{minTemp:F0} - {maxTemp:F0}°C";
        return code switch
        {
            0 => $"Trời quang đãng, nắng ấm ({tempStr}, xác suất mưa {precipProb}%)",
            1 or 2 or 3 => $"Trời nhiều mây, nắng gián đoạn ({tempStr}, xác suất mưa {precipProb}%)",
            45 or 48 => $"Sương mù mát lạnh ({tempStr}, xác suất mưa {precipProb}%)",
            51 or 53 or 55 => $"Mưa phùn lất phất ({tempStr}, xác suất mưa {precipProb}%)",
            61 or 63 or 65 => $"Có mưa rào ({tempStr}, xác suất mưa {precipProb}%)",
            80 or 81 or 82 => $"Mưa rào từng đợt ({tempStr}, xác suất mưa {precipProb}%)",
            95 or 96 or 99 => $"Cảnh báo dông sét, mưa to ({tempStr}, gió giật mạnh)",
            _ => $"Khô ráo mát mẻ ({tempStr}, xác suất mưa {precipProb}%)"
        };
    }

    private class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public DailyData? Daily { get; set; }
    }

    private class DailyData
    {
        [JsonPropertyName("time")]
        public List<string>? Time { get; set; }

        [JsonPropertyName("weather_code")]
        public List<int>? WeatherCode { get; set; }

        [JsonPropertyName("temperature_2m_max")]
        public List<double>? TemperatureMax { get; set; }

        [JsonPropertyName("temperature_2m_min")]
        public List<double>? TemperatureMin { get; set; }

        [JsonPropertyName("precipitation_sum")]
        public List<double>? PrecipitationSum { get; set; }

        [JsonPropertyName("precipitation_probability_max")]
        public List<int>? PrecipitationProbabilityMax { get; set; }

        [JsonPropertyName("wind_speed_10m_max")]
        public List<double>? WindSpeedMax { get; set; }

        [JsonPropertyName("uv_index_max")]
        public List<double>? UvIndexMax { get; set; }
    }
}
