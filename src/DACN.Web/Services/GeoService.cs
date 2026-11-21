using System.Globalization;

namespace DACN.Web.Services;

public interface IGeoService
{
    (double Lat, double Lng) LamTronToaDo(double lat, double lng);
    double TinhKhoangCachHaversineKm(double lat1, double lng1, double lat2, double lng2);
    (double KmDuongBo, int ThoiGianPhut, decimal ChiPhiDiChuyenUocTinhVnd, string PhuongTien) UocTinhDiChuyen(
        double haversineKm, bool laDeoNui, bool laBienDao, string? vungXuatPhat = "south");
    (double Lat, double Lng, string TenChuan) PhanGiaiDiemXuatPhat(double? gpsLat, double? gpsLng, string? tenThanhPho);
    double LayBanKinhToiDaTheoSoNgay(int soNgay);
    Task<(double Lat, double Lng, string VungMien, string DisplayName)?> TraCuuToaDoTuDiaChiAsync(
        string diaChi, string? tinhThanh = null, CancellationToken cancellationToken = default);
}

public class GeoService : IGeoService
{
    private readonly IHttpClientFactory? _httpClientFactory;

    public GeoService(IHttpClientFactory? httpClientFactory = null)
    {
        _httpClientFactory = httpClientFactory;
    }

    // Bảng tọa độ trung tâm hành chính 63 Tỉnh/Thành phố Việt Nam (Offline 100%, 0ms latency)
    private static readonly Dictionary<string, (double Lat, double Lng, string TenChuan, string VungMien)> TinhThanhDict =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Miền Nam
            ["cu chi"] = (10.9733, 106.4933, "Huyện Củ Chi, TP. Hồ Chí Minh", "south"),
            ["cuchi"] = (10.9733, 106.4933, "Huyện Củ Chi, TP. Hồ Chí Minh", "south"),
            ["can gio"] = (10.4578, 106.8942, "Huyện Cần Giờ, TP. Hồ Chí Minh", "south"),
            ["cangio"] = (10.4578, 106.8942, "Huyện Cần Giờ, TP. Hồ Chí Minh", "south"),
            ["hcm"] = (10.7769, 106.7009, "TP. Hồ Chí Minh", "south"),
            ["ho chi minh"] = (10.7769, 106.7009, "TP. Hồ Chí Minh", "south"),
            ["tp hcm"] = (10.7769, 106.7009, "TP. Hồ Chí Minh", "south"),
            ["tphcm"] = (10.7769, 106.7009, "TP. Hồ Chí Minh", "south"),
            ["sai gon"] = (10.7769, 106.7009, "TP. Hồ Chí Minh", "south"),
            ["saigon"] = (10.7769, 106.7009, "TP. Hồ Chí Minh", "south"),
            ["vung tau"] = (10.3460, 107.0843, "Bà Rịa - Vũng Tàu", "south"),
            ["ba ria"] = (10.4962, 107.1694, "Bà Rịa - Vũng Tàu", "south"),
            ["dong nai"] = (10.9574, 106.8427, "Đồng Nai", "south"),
            ["bien hoa"] = (10.9574, 106.8427, "Đồng Nai", "south"),
            ["binh duong"] = (11.1666, 106.6333, "Bình Dương", "south"),
            ["thu dau mot"] = (10.9805, 106.6519, "Bình Dương", "south"),
            ["can tho"] = (10.0452, 105.7469, "Cần Thơ", "south"),
            ["kien giang"] = (10.0125, 105.0809, "Kiên Giang", "south"),
            ["phu quoc"] = (10.2289, 103.9572, "Phú Quốc", "south"),
            ["an giang"] = (10.5216, 105.1259, "An Giang", "south"),
            ["tien giang"] = (10.4493, 106.3422, "Tiền Giang", "south"),
            ["my tho"] = (10.3600, 106.3600, "Tiền Giang", "south"),
            ["ben tre"] = (10.2433, 106.3756, "Bến Tre", "south"),
            ["dong thap"] = (10.4578, 105.6331, "Đồng Tháp", "south"),
            ["vinh long"] = (10.2537, 105.9722, "Vĩnh Long", "south"),
            ["tra vinh"] = (9.9347, 106.3456, "Trà Vinh", "south"),
            ["soc trang"] = (9.6033, 105.9800, "Sóc Trăng", "south"),
            ["bac lieu"] = (9.2941, 105.7278, "Bạc Liêu", "south"),
            ["ca mau"] = (9.1769, 105.1500, "Cà Mau", "south"),
            ["tay ninh"] = (11.3100, 106.0983, "Tây Ninh", "south"),
            ["binh phuoc"] = (11.7511, 106.7236, "Bình Phước", "south"),
            ["long an"] = (10.5333, 106.4000, "Long An", "south"),

            // Miền Trung & Tây Nguyên
            ["da lat"] = (11.9404, 108.4583, "Lâm Đồng", "south"),
            ["dalat"] = (11.9404, 108.4583, "Lâm Đồng", "south"),
            ["lam dong"] = (11.9404, 108.4583, "Lâm Đồng", "south"),
            ["bao loc"] = (11.5478, 107.8089, "Lâm Đồng", "south"),
            ["dak lak"] = (12.6667, 108.0500, "Đắk Lắk", "central"),
            ["buon ma thuot"] = (12.6667, 108.0500, "Đắk Lắk", "central"),
            ["gia lai"] = (13.9833, 108.0000, "Gia Lai", "central"),
            ["pleiku"] = (13.9833, 108.0000, "Gia Lai", "central"),
            ["kon tum"] = (14.3500, 108.0000, "Kon Tum", "central"),
            ["dak nong"] = (12.0000, 107.6833, "Đắk Nông", "central"),
            ["nha trang"] = (12.2388, 109.1967, "Khánh Hòa", "central"),
            ["khanh hoa"] = (12.2388, 109.1967, "Khánh Hòa", "central"),
            ["phan thiet"] = (10.9333, 108.1000, "Bình Thuận", "central"),
            ["binh thuan"] = (10.9333, 108.1000, "Bình Thuận", "central"),
            ["mui ne"] = (10.9333, 108.2833, "Bình Thuận", "central"),
            ["ninh thuan"] = (11.5667, 108.9833, "Ninh Thuận", "central"),
            ["phan rang"] = (11.5667, 108.9833, "Ninh Thuận", "central"),
            ["phu yen"] = (13.0889, 109.3139, "Phú Yên", "central"),
            ["tuy hoa"] = (13.0889, 109.3139, "Phú Yên", "central"),
            ["binh dinh"] = (13.7833, 109.2167, "Bình Định", "central"),
            ["quy nhon"] = (13.7833, 109.2167, "Bình Định", "central"),
            ["quang ngai"] = (15.1200, 108.8000, "Quảng Ngãi", "central"),
            ["quang nam"] = (15.5667, 108.4833, "Quảng Nam", "central"),
            ["hoi an"] = (15.8801, 108.3274, "Quảng Nam", "central"),
            ["da nang"] = (16.0544, 108.2022, "Đà Nẵng", "central"),
            ["danang"] = (16.0544, 108.2022, "Đà Nẵng", "central"),
            ["dn"] = (16.0544, 108.2022, "Đà Nẵng", "central"),
            ["hue"] = (16.4637, 107.5909, "Thừa Thiên Huế", "central"),
            ["thua thien hue"] = (16.4637, 107.5909, "Thừa Thiên Huế", "central"),
            ["quang tri"] = (16.7500, 107.1833, "Quảng Trị", "central"),
            ["quang binh"] = (17.4833, 106.6000, "Quảng Bình", "central"),
            ["dong hoi"] = (17.4833, 106.6000, "Quảng Bình", "central"),
            ["ha tinh"] = (18.3333, 105.9000, "Hà Tĩnh", "central"),
            ["nghe an"] = (18.6667, 105.6667, "Nghệ An", "central"),
            ["vinh"] = (18.6667, 105.6667, "Nghệ An", "central"),
            ["thanh hoa"] = (19.8000, 105.7667, "Thanh Hóa", "central"),

            // Miền Bắc
            ["ha noi"] = (21.0285, 105.8542, "Hà Nội", "north"),
            ["hanoi"] = (21.0285, 105.8542, "Hà Nội", "north"),
            ["hn"] = (21.0285, 105.8542, "Hà Nội", "north"),
            ["hai phong"] = (20.8561, 106.6822, "Hải Phòng", "north"),
            ["haiphong"] = (20.8561, 106.6822, "Hải Phòng", "north"),
            ["quang ninh"] = (20.9500, 107.0833, "Quảng Ninh", "north"),
            ["ha long"] = (20.9500, 107.0833, "Quảng Ninh", "north"),
            ["ninh binh"] = (20.2500, 105.9750, "Ninh Bình", "north"),
            ["nam dinh"] = (20.4167, 106.1667, "Nam Định", "north"),
            ["thai binh"] = (20.4500, 106.3333, "Thái Bình", "north"),
            ["hai duong"] = (20.9333, 106.3167, "Hải Dương", "north"),
            ["hung yen"] = (20.6500, 106.0500, "Hưng Yên", "north"),
            ["ha nam"] = (20.5833, 105.9167, "Hà Nam", "north"),
            ["bac ninh"] = (21.1833, 106.0667, "Bắc Ninh", "north"),
            ["bac giang"] = (21.2667, 106.2000, "Bắc Giang", "north"),
            ["vinh phuc"] = (21.3000, 105.6000, "Vĩnh Phúc", "north"),
            ["phu tho"] = (21.3167, 105.2167, "Phú Thọ", "north"),
            ["thai nguyen"] = (21.5833, 105.8333, "Thái Nguyên", "north"),
            ["lang son"] = (21.8500, 106.7500, "Lạng Sơn", "north"),
            ["tuyen quang"] = (21.8167, 105.2167, "Tuyên Quang", "north"),
            ["ha giang"] = (22.8167, 104.9833, "Hà Giang", "north"),
            ["cao bang"] = (22.6667, 106.2500, "Cao Bằng", "north"),
            ["bac kan"] = (22.1333, 105.8333, "Bắc Kạn", "north"),
            ["lao cai"] = (22.4833, 103.9667, "Lào Cai", "north"),
            ["sa pa"] = (22.3364, 103.8438, "Lào Cai", "north"),
            ["sapa"] = (22.3364, 103.8438, "Lào Cai", "north"),
            ["yen bai"] = (21.7000, 104.8667, "Yên Bái", "north"),
            ["son la"] = (21.3167, 103.9000, "Sơn La", "north"),
            ["moc chau"] = (20.8333, 104.6333, "Sơn La", "north"),
            ["dien bien"] = (21.3833, 103.0167, "Điện Biên", "north"),
            ["lai chau"] = (22.3833, 103.4667, "Lai Châu", "north"),
            ["hoa binh"] = (20.8167, 105.3333, "Hòa Bình", "north")
        };

    public (double Lat, double Lng) LamTronToaDo(double lat, double lng)
    {
        // Làm tròn 2 chữ số thập phân (~1.1 km) để bảo vệ quyền riêng tư người dùng
        return (Math.Round(lat, 2), Math.Round(lng, 2));
    }

    public double TinhKhoangCachHaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0; // Bán kính Trái Đất theo km
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLng / 2.0) * Math.Sin(dLng / 2.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return Math.Round(R * c, 1);
    }

    public (double KmDuongBo, int ThoiGianPhut, decimal ChiPhiDiChuyenUocTinhVnd, string PhuongTien) UocTinhDiChuyen(
        double haversineKm, bool laDeoNui, bool laBienDao, string? vungXuatPhat = "south")
    {
        if (laBienDao)
        {
            // Điểm đến là đảo (Phú Quốc) -> Cần máy bay hoặc tàu cao tốc khứ hồi
            var chiPhiVeKhuHoi = string.Equals(vungXuatPhat, "north", StringComparison.OrdinalIgnoreCase)
                ? 2_800_000m
                : (string.Equals(vungXuatPhat, "central", StringComparison.OrdinalIgnoreCase) ? 2_200_000m : 1_650_000m);

            return (
                KmDuongBo: Math.Round(haversineKm * 1.15, 1),
                ThoiGianPhut: 90, // Bay ~1h + thủ tục sân bay
                ChiPhiDiChuyenUocTinhVnd: chiPhiVeKhuHoi,
                PhuongTien: "Máy bay khứ hồi (~1h bay) hoặc Xe khách + Tàu cao tốc Rạch Giá"
            );
        }

        // Đường bộ thông thường
        var heSoDuongBo = laDeoNui ? 1.38 : 1.30;
        var kmDuongBo = Math.Round(haversineKm * heSoDuongBo, 1);

        // Vận tốc trung bình: đồng bằng cao tốc ~60km/h, đèo núi ~45km/h
        var vanToc = laDeoNui ? 45.0 : 60.0;
        var thoiGianPhut = (int)Math.Round((kmDuongBo / vanToc) * 60.0);

        // Ước tính chi phí khứ hồi: xe bus/limousine/xăng xe
        decimal chiPhiUocTinh;
        string phuongTien;

        if (kmDuongBo <= 130)
        {
            chiPhiUocTinh = 350_000m; // Xe khách Hoa Mai / Kumho Vũng Tàu khứ hồi
            phuongTien = $"Xe ô tô / Xe Limousine / Xe máy (~{thoiGianPhut / 60}h{thoiGianPhut % 60:D2}p)";
        }
        else if (kmDuongBo <= 350)
        {
            chiPhiUocTinh = 680_000m; // Xe Limousine giường nằm Đà Lạt khứ hồi
            phuongTien = $"Xe khách giường nằm cao cấp / Xe Limousine (~{thoiGianPhut / 60}h{thoiGianPhut % 60:D2}p)";
        }
        else
        {
            chiPhiUocTinh = 2_200_000m; // Vé máy bay khứ hồi đường dài (Đà Nẵng/Hà Nội)
            phuongTien = $"Máy bay khứ hồi / Tàu hỏa (~{Math.Max(1, thoiGianPhut / 120)}h)";
        }

        return (kmDuongBo, thoiGianPhut, chiPhiUocTinh, phuongTien);
    }

    public (double Lat, double Lng, string TenChuan) PhanGiaiDiemXuatPhat(double? gpsLat, double? gpsLng, string? tenThanhPho)
    {
        // 1. Ưu tiên tên thành phố do người dùng nhập hoặc trích xuất từ văn bản
        if (!string.IsNullOrWhiteSpace(tenThanhPho))
        {
            var cleanKey = ChuanHoaChuoi(tenThanhPho);
            foreach (var kvp in TinhThanhDict)
            {
                if (cleanKey.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains(cleanKey, StringComparison.OrdinalIgnoreCase))
                {
                    return (kvp.Value.Lat, kvp.Value.Lng, kvp.Value.TenChuan);
                }
            }
        }

        // 2. Nếu có tọa độ GPS hợp lệ (trong lãnh thổ Việt Nam: Lat 8..24, Lng 102..110)
        if (gpsLat.HasValue && gpsLng.HasValue &&
            gpsLat.Value >= 8.0 && gpsLat.Value <= 24.0 &&
            gpsLng.Value >= 102.0 && gpsLng.Value <= 110.0)
        {
            var rounded = LamTronToaDo(gpsLat.Value, gpsLng.Value);

            // Tìm tỉnh thành gần nhất với tọa độ GPS
            string tenGanNhat = "Vị trí của bạn";
            double minKm = double.MaxValue;

            foreach (var kvp in TinhThanhDict)
            {
                var d = TinhKhoangCachHaversineKm(rounded.Lat, rounded.Lng, kvp.Value.Lat, kvp.Value.Lng);
                if (d < minKm)
                {
                    minKm = d;
                    tenGanNhat = kvp.Value.TenChuan;
                }
            }

            return (rounded.Lat, rounded.Lng, minKm <= 40 ? tenGanNhat : "Vị trí GPS của bạn");
        }

        // 3. Mặc định là TP. Hồ Chí Minh nếu không có thông tin
        return (10.7769, 106.7009, "TP. Hồ Chí Minh");
    }

    public double LayBanKinhToiDaTheoSoNgay(int soNgay)
    {
        if (soNgay <= 1) return 150.0;  // Đi về trong ngày: tối đa 150km (Vũng Tàu, Tây Ninh, Cần Giờ)
        if (soNgay <= 3) return 330.0;  // Cuối tuần 2N1Đ - 3N2Đ: tối đa ~330km (Đà Lạt, Phan Thiết)
        return 1200.0;                  // Dài ngày (>= 4 ngày): không giới hạn bán kính
    }

    private static string ChuanHoaChuoi(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Replace('đ', 'd').Replace('Đ', 'd');
        var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Trim();
    }

    private static readonly string[] UuTienDacBiet =
    [
        "cu chi", "cuchi",
        "can gio", "cangio",
        "phu quoc", "phuquoc",
        "con dao", "condao",
        "cat ba", "catba",
        "sa pa", "sapa",
        "hoi an", "hoian",
        "da lat", "dalat",
        "vung tau", "vungtau",
        "hai phong", "haiphong",
        "da nang", "danang",
        "do son", "doson"
    ];

    public async Task<(double Lat, double Lng, string VungMien, string DisplayName)?> TraCuuToaDoTuDiaChiAsync(
        string diaChi, string? tinhThanh = null, CancellationToken cancellationToken = default)
    {
        var rawText = $"{diaChi} {tinhThanh}".Trim();
        if (string.IsNullOrWhiteSpace(rawText)) return null;

        var clean = ChuanHoaChuoi(rawText);

        // 1. Kiểm tra nhóm địa danh du lịch/quận huyện trọng điểm trước (tránh nhầm huyện với tỉnh cha)
        foreach (var spec in UuTienDacBiet)
        {
            var cleanSpec = ChuanHoaChuoi(spec);
            if (clean.Contains(cleanSpec) && TinhThanhDict.TryGetValue(spec, out var specVal))
            {
                return (specVal.Lat, specVal.Lng, specVal.VungMien, specVal.TenChuan);
            }
        }

        // 2. Kiểm tra từ điển nội bộ 63 tỉnh thành (ưu tiên key dài trước)
        var sortedKeys = TinhThanhDict.Keys.OrderByDescending(k => k.Length);
        foreach (var k in sortedKeys)
        {
            var cleanKey = ChuanHoaChuoi(k);
            if (cleanKey.Length <= 3)
            {
                // Key ngắn (dn, hn, hcm) chỉ khớp khi là từ riêng biệt
                var tokens = clean.Split(new[] { ' ', ',', '-', '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Contains(cleanKey))
                {
                    var val = TinhThanhDict[k];
                    return (val.Lat, val.Lng, val.VungMien, val.TenChuan);
                }
            }
            else if (clean.Contains(cleanKey))
            {
                var val = TinhThanhDict[k];
                return (val.Lat, val.Lng, val.VungMien, val.TenChuan);
            }
        }

        // 3. Tra cứu Online qua Open-Meteo Geocoding API (miễn phí, không cần API key)
        try
        {
            var searchKey = !string.IsNullOrWhiteSpace(tinhThanh) ? tinhThanh.Trim() : diaChi.Trim();
            var client = _httpClientFactory?.CreateClient() ?? new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(4);
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(searchKey)}&count=1&language=vi&format=json";

            using var resp = await client.GetAsync(url, cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = await System.Text.Json.JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var first = results[0];
                    var lat = first.GetProperty("latitude").GetDouble();
                    var lng = first.GetProperty("longitude").GetDouble();
                    var name = first.TryGetProperty("name", out var n) ? n.GetString() ?? searchKey : searchKey;
                    var admin1 = first.TryGetProperty("admin1", out var a) ? a.GetString() ?? "" : "";

                    var vungMien = lat switch
                    {
                        >= 19.5 => "north",
                        >= 11.5 => "central",
                        _ => "south"
                    };

                    var displayName = string.IsNullOrEmpty(admin1) ? name : $"{name}, {admin1}";
                    return (Math.Round(lat, 4), Math.Round(lng, 4), vungMien, displayName);
                }
            }
        }
        catch
        {
            // Khi offline hoặc lỗi mạng, tự động fallback
        }

        // 3. Fallback mặc định theo miền
        return (10.7769, 106.7009, "south", "TP. Hồ Chí Minh");
    }
}
