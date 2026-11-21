using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DACN.Web.Services;

public class GoiYDuLichRequest
{
    public decimal? NganSach { get; set; }
    public int? ThangDi { get; set; }
    public string? PhongCach { get; set; } // songao, thugian, phieuluu, amthuc, giadinh, all
    public string? KhuVuc { get; set; } // north, central, south, all
    public string? DiemXuatPhat { get; set; } // hcm, hn, dn, vungtau...
    public bool YeuCauGanXuatPhat { get; set; }
    public string? DiemDenCuThe { get; set; } // vungtau, dalat, phuquoc, danang, hoian
    public int SoDem { get; set; } = 2;
    public int SoKhach { get; set; } = 2;
    public double? ViDoXuatPhat { get; set; }
    public double? KinhDoXuatPhat { get; set; }
    public string? ThanhPhoXuatPhat { get; set; }
    public DateTime? NgayDi { get; set; }
}

public class GoiYKetQuaDto
{
    public int MaTourHoacDiaDiem { get; set; }
    public string LoaiGoiY { get; set; } = "Tour & Điểm đến";
    public string TieuDe { get; set; } = string.Empty;
    public string DiemDen { get; set; } = string.Empty;
    public string KhuVuc { get; set; } = "central";
    public string TenMuaDepNhat { get; set; } = string.Empty;
    public string ThoiDiemDepNhat { get; set; } = string.Empty;
    public string ThoiTietDuKien { get; set; } = string.Empty;
    public string NguonThoiTiet { get; set; } = "Ước lượng theo mùa";
    public bool LaDungMua { get; set; } = true;
    public int KhoangCachTuHcmKm { get; set; } = 300;
    public string PhuongTienGoiY { get; set; } = "Xe Limousine giường nằm / Tự lái (~6 giờ)";
    public decimal ChiPhiDiChuyenUocTinh { get; set; } = 0;
    public decimal ChiPhiDuKienTu { get; set; }
    public decimal ChiPhiDuKienDen { get; set; }
    public int SoNgayGoiY { get; set; } = 3;
    public List<string> DanhSachGu { get; set; } = new();
    public string LyDoAiGoiY { get; set; } = string.Empty;
    public string DiemCheckInNoiBat { get; set; } = string.Empty;
    public string? DuongDanAnh { get; set; }
    public int PhanTramPhuHop { get; set; }
    public string? HangPhongPhuHop { get; set; }
    public decimal? GiaPhongMoiDem { get; set; }
    public int MaChiNhanh { get; set; } = 1;
    public string TenChiNhanh { get; set; } = "GrandStay Đà Nẵng Beach Resort";
    public bool LaDiemTrongNha { get; set; } = false;
    public decimal DiemBienTap { get; set; } = 4.5m;
}

public class TrichXuatYChinhResult
{
    public string ChuDe { get; set; } = "goi_y_du_lich"; // goi_y_du_lich, gia_phong, voucher
    public decimal? NganSach { get; set; }
    public int? ThangDi { get; set; }
    public string? PhongCach { get; set; }
    public string? KhuVuc { get; set; }
    public string? DiemXuatPhat { get; set; }
    public bool YeuCauGanXuatPhat { get; set; }
    public string? DiemDenCuThe { get; set; }
    public int SoDem { get; set; } = 2;
    public double? ViDoXuatPhat { get; set; }
    public double? KinhDoXuatPhat { get; set; }
    public string? ThanhPhoXuatPhat { get; set; }
    public DateTime? NgayDi { get; set; }
}

public class ChatAiKetQuaDto
{
    public string MaPhienChat { get; set; } = string.Empty;
    public string CauTraLoi { get; set; } = string.Empty;
    public string NguonTraLoi { get; set; } = "Hybrid-Rule-DB";
    public TrichXuatYChinhResult YChinh { get; set; } = new();
    public List<GoiYKetQuaDto> GoiYKemTheo { get; set; } = new();
    public DateTime ThoiGian { get; set; }
}

public class GoiYDuLichService
{
    private class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    private readonly DACNContext db;
    private readonly IGeoService _geoService;
    private readonly IWeatherService _weatherService;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration config;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<GoiYDuLichService> logger;

    public GoiYDuLichService(
        DACNContext db,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<GoiYDuLichService> logger)
        : this(db, new GeoService(), null, TimeProvider.System, config, httpClientFactory, logger)
    {
    }

    public GoiYDuLichService(
        DACNContext db,
        IGeoService? geoService = null,
        IWeatherService? weatherService = null,
        TimeProvider? timeProvider = null,
        IConfiguration? config = null,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<GoiYDuLichService>? logger = null)
    {
        this.db = db;
        _geoService = geoService ?? new GeoService();
        _timeProvider = timeProvider ?? TimeProvider.System;
        this.config = config ?? new ConfigurationBuilder().Build();
        this.httpClientFactory = httpClientFactory ?? new DefaultHttpClientFactory();
        this.logger = logger ?? NullLogger<GoiYDuLichService>.Instance;
        _weatherService = weatherService ?? new WeatherService(this.httpClientFactory, new MemoryCache(new MemoryCacheOptions()), _timeProvider, NullLogger<WeatherService>.Instance);
    }

    public static bool ThangNamTrongKhoang(int thang, int batDau, int ketThuc)
    {
        if (thang < 1 || thang > 12) return false;
        if (batDau <= ketThuc)
            return thang >= batDau && thang <= ketThuc;
        // Mùa vắt qua năm mới (ví dụ tháng 11 -> tháng 2)
        return thang >= batDau || thang <= ketThuc;
    }

    public static decimal TinhDiemMua(int thang, int batDau, int ketThuc, decimal diemGoc = 0.92m)
    {
        if (ThangNamTrongKhoang(thang, batDau, ketThuc))
            return Math.Clamp(diemGoc > 1m ? diemGoc / 1000m : diemGoc, 0.85m, 1.0m);

        int Dist(int a, int b)
        {
            var d = Math.Abs(a - b);
            return Math.Min(d, 12 - d);
        }

        var minDist = Math.Min(Dist(thang, batDau), Dist(thang, ketThuc));
        // Phạt nặng các tháng trái mùa (cách >= 2 tháng giảm xuống <= 0.30m để không bao giờ gợi ý điểm đến mùa mưa/trái mùa)
        return Math.Max(0.20m, 0.72m - minDist * 0.22m);
    }

    public static decimal TinhDiemViTriVaDiChuyen(int branchId, string? diemXuatPhat, bool yeuCauGan, decimal? nganSach)
    {
        if (string.Equals(diemXuatPhat, "hcm", StringComparison.OrdinalIgnoreCase))
        {
            if (branchId == 5) // Củ Chi: Sát vách TP.HCM (~45km), đi về trong ngày siêu tiện
                return 1.0m;

            if (branchId == 4) // Vũng Tàu: Cực gần TP.HCM (~105km cao tốc), xe khách khứ hồi chỉ 350k
                return 1.0m;

            if (branchId == 3) // Đà Lạt: Cách TP.HCM ~300km đường bộ, xe Limousine khứ hồi chỉ ~650k
                return yeuCauGan ? 0.95m : 1.0m;

            if (branchId == 2) // Phú Quốc: Cùng miền Nam nhưng cần máy bay / tàu cao tốc ra đảo
            {
                if (yeuCauGan && nganSach.HasValue && nganSach.Value <= 3_300_000m)
                    return 0.55m;
                return yeuCauGan ? 0.78m : 0.92m;
            }

            if (branchId == 6) // Hải Phòng: Miền Bắc
                return yeuCauGan ? 0.20m : 0.70m;

            // Đà Nẵng / Hội An: Cách TP.HCM >900km, tốn vé máy bay khứ hồi đắt đỏ
            return yeuCauGan ? 0.15m : (nganSach.HasValue && nganSach.Value <= 3_500_000m ? 0.25m : 0.60m);
        }

        return 0.95m;
    }

    public static (int KhoangCachHcmKm, string PhuongTienTuHcm, decimal ChiPhiDiChuyenTuHcm) LayThongTinDiChuyenTuHcm(int branchId) => branchId switch
    {
        6 => (1750, "Máy bay khứ hồi (~2 giờ bay từ TP.HCM)", 2_500_000m),
        5 => (45, "Xe máy / Xe buýt / Ô tô (~1 giờ từ TP.HCM)", 100_000m),
        4 => (105, "Xe ô tô cao tốc / Xe khách Hoa Mai (~2 giờ từ TP.HCM)", 350_000m),
        3 => (300, "Xe giường nằm Limousine / Tự lái (~6 giờ từ TP.HCM)", 650_000m),
        2 => (400, "Máy bay 1 giờ hoặc Xe + Tàu cao tốc từ TP.HCM", 1_600_000m),
        _ => (920, "Máy bay 1h20p từ TP.HCM (Cách >900km)", 2_200_000m)
    };

    public static decimal TinhDiemNganSach(decimal? nganSachKhach, decimal giaToiThieu, decimal giaToiDa)
    {
        if (!nganSachKhach.HasValue || nganSachKhach.Value <= 0)
            return 0.90m;

        var budget = nganSachKhach.Value;
        if (budget >= giaToiThieu && budget <= giaToiDa * 1.25m)
            return 1.0m;
        if (budget >= giaToiThieu)
            return 0.92m;

        var ratio = budget / Math.Max(1m, giaToiThieu);
        return Math.Clamp(ratio, 0.25m, 0.95m);
    }

    public static decimal TinhDiemGu(string? guKhach, IReadOnlyCollection<string> guDiemDen)
    {
        if (string.IsNullOrWhiteSpace(guKhach) || guKhach.Equals("all", StringComparison.OrdinalIgnoreCase))
            return 0.90m;

        var requested = guKhach.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToList();

        if (requested.Count == 0) return 0.90m;

        var matched = requested.Count(r => guDiemDen.Any(g => g.Equals(r, StringComparison.OrdinalIgnoreCase)));
        return matched > 0 ? Math.Min(1.0m, 0.75m + 0.25m * matched / requested.Count) : 0.45m;
    }

    public static List<string> PhanTichGu(string? phongCachColumn, string? moTa, string? tieuDe)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(phongCachColumn))
        {
            foreach (var p in phongCachColumn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                tags.Add(p.ToLowerInvariant());
        }

        var combined = $"{tieuDe} {moTa}".ToLowerInvariant();
        if (combined.Contains("sống ảo") || combined.Contains("check-in") || combined.Contains("cầu rồng") || combined.Contains("hoàng hôn") || combined.Contains("bà nà") || combined.Contains("phố cổ"))
            tags.Add("songao");
        if (combined.Contains("thư giãn") || combined.Contains("nghỉ dưỡng") || combined.Contains("biển") || combined.Contains("spa") || combined.Contains("yên bình"))
            tags.Add("thugian");
        if (combined.Contains("khám phá") || combined.Contains("săn mây") || combined.Contains("bán đảo") || combined.Contains("đèo") || combined.Contains("lặn"))
            tags.Add("phieuluu");
        if (combined.Contains("hải sản") || combined.Contains("ẩm thực") || combined.Contains("chợ đêm"))
            tags.Add("amthuc");
        if (combined.Contains("gia đình") || combined.Contains("family"))
            tags.Add("giadinh");

        if (tags.Count == 0)
        {
            tags.Add("songao");
            tags.Add("thugian");
        }

        return tags.ToList();
    }

    public static TrichXuatYChinhResult TrichXuatYChinh(string cauHoi)
    {
        var q = (cauHoi ?? string.Empty).Trim().ToLowerInvariant();
        var res = new TrichXuatYChinhResult();

        if (q.Contains("voucher") || q.Contains("mã giảm") || q.Contains("khuyến mãi") || q.Contains("ưu đãi"))
            res.ChuDe = "voucher";
        else if ((q.Contains("giá phòng") || q.Contains("phòng trống") || q.Contains("hạng phòng")) && !q.Contains("đi đâu"))
            res.ChuDe = "gia_phong";
        else
            res.ChuDe = "goi_y_du_lich";

        // 1. Trích xuất ngân sách (VD: "2 triệu", "2tr5", "2.5 triệu", "3000000", "500k")
        var trMatch = Regex.Match(q, @"(\d+(?:[.,]\d+)?)\s*(?:triệu|trieu|tr|củ)(\s*\d+)?");
        if (trMatch.Success && decimal.TryParse(trMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var trieu))
        {
            var extra = 0m;
            if (trMatch.Groups[2].Success && decimal.TryParse(trMatch.Groups[2].Value.Trim(), out var le))
                extra = le * 100_000m;
            res.NganSach = trieu * 1_000_000m + extra;
        }
        else
        {
            var kMatch = Regex.Match(q, @"(\d{3,4})\s*k\b");
            if (kMatch.Success && decimal.TryParse(kMatch.Groups[1].Value, out var nghin))
                res.NganSach = nghin * 1_000m;
            else
            {
                var rawNum = Regex.Match(q, @"\b(\d{1,2})[.,](\d{3})[.,](\d{3})\b");
                if (rawNum.Success && decimal.TryParse(rawNum.Groups[1].Value + rawNum.Groups[2].Value + rawNum.Groups[3].Value, out var vnd))
                    res.NganSach = vnd;
            }
        }

        // 2. Trích xuất tháng / mùa
        var monthMatch = Regex.Match(q, @"tháng\s*(\d{1,2})");
        if (monthMatch.Success && int.TryParse(monthMatch.Groups[1].Value, out var m) && m >= 1 && m <= 12)
            res.ThangDi = m;
        else if (q.Contains("mùa hè") || q.Contains("hè này"))
            res.ThangDi = 6;
        else if (q.Contains("mùa thu"))
            res.ThangDi = 9;
        else if (q.Contains("mùa đông") || q.Contains("cuối năm") || q.Contains("noel") || q.Contains("giáng sinh"))
            res.ThangDi = 12;
        else if (q.Contains("tết") || q.Contains("mùa xuân") || q.Contains("đầu năm"))
            res.ThangDi = 2;

        // 3. Trích xuất số đêm / số ngày (VD: "3 ngày 2 đêm", "2n1đ", "3n2đ", "2 đêm")
        var nightMatch = Regex.Match(q, @"(\d)\s*(?:đêm|dem|đ\b|d\b)");
        if (nightMatch.Success && int.TryParse(nightMatch.Groups[1].Value, out var nights) && nights >= 1 && nights <= 14)
            res.SoDem = nights;

        // 4. Trích xuất Gu trải nghiệm
        var vibes = new List<string>();
        if (q.Contains("sống ảo") || q.Contains("check-in") || q.Contains("checkin") || q.Contains("chụp ảnh") || q.Contains("đẹp"))
            vibes.Add("songao");
        if (q.Contains("thư giãn") || q.Contains("chữa lành") || q.Contains("chill") || q.Contains("nghỉ dưỡng") || q.Contains("yên tĩnh"))
            vibes.Add("thugian");
        if (q.Contains("săn mây") || q.Contains("khám phá") || q.Contains("phiêu lưu") || q.Contains("leo núi"))
            vibes.Add("phieuluu");
        if (q.Contains("ăn uống") || q.Contains("ẩm thực") || q.Contains("hải sản"))
            vibes.Add("amthuc");
        if (q.Contains("gia đình") || q.Contains("con nhỏ"))
            vibes.Add("giadinh");

        if (vibes.Count > 0)
            res.PhongCach = string.Join(",", vibes);

        // 5. Trích xuất Điểm xuất phát, Khoảng cách & Khu vực địa lý
        if (q.Contains("gần") || q.Contains("quanh") || q.Contains("lân cận") || q.Contains("xung quanh") || q.Contains("không xa"))
            res.YeuCauGanXuatPhat = true;

        var mentionsHcm = q.Contains("hồ chí minh") || q.Contains("ho chi minh") || q.Contains("tp.hcm") || q.Contains("tphcm") || q.Contains("hcm") || q.Contains("sài gòn") || q.Contains("saigon");
        if (mentionsHcm)
        {
            res.DiemXuatPhat = "hcm";
            res.KhuVuc = "south";
        }

        if (q.Contains("củ chi") || q.Contains("cu chi"))
        {
            res.DiemDenCuThe = "cuchi";
            res.KhuVuc = "south";
            if (!nightMatch.Success) res.SoDem = 1;
        }
        else if (q.Contains("hải phòng") || q.Contains("hai phong"))
        {
            res.DiemDenCuThe = "haiphong";
            res.KhuVuc = "north";
        }
        else if (q.Contains("cần giờ") || q.Contains("can gio"))
        {
            res.DiemDenCuThe = "cangio";
            res.KhuVuc = "south";
            if (!nightMatch.Success) res.SoDem = 1;
        }
        else if (q.Contains("tây ninh") || q.Contains("tay ninh"))
        {
            res.DiemDenCuThe = "tayninh";
            res.KhuVuc = "south";
            if (!nightMatch.Success) res.SoDem = 1;
        }
        else if (q.Contains("vũng tàu") || q.Contains("vung tau") || q.Contains("bà rịa") || q.Contains("hồ tràm") || q.Contains("bãi sau"))
        {
            res.DiemDenCuThe = "vungtau";
            res.KhuVuc = "south";
        }
        else if (q.Contains("đà lạt") || q.Contains("da lat") || q.Contains("lâm đồng") || q.Contains("cầu đất") || q.Contains("tuyền lâm"))
        {
            res.DiemDenCuThe = "dalat";
            res.KhuVuc = "south";
        }
        else if (q.Contains("phú quốc") || q.Contains("phu quoc") || q.Contains("kiên giang") || q.Contains("bãi trường"))
        {
            res.DiemDenCuThe = "phuquoc";
            res.KhuVuc = "south";
        }
        else if (q.Contains("đà nẵng") || q.Contains("da nang") || q.Contains("mỹ khê") || q.Contains("sơn trà") || q.Contains("bà nà"))
        {
            res.DiemDenCuThe = "danang";
            res.KhuVuc = "central";
        }
        else if (q.Contains("hội an") || q.Contains("hoi an") || q.Contains("quảng nam"))
        {
            res.DiemDenCuThe = "hoian";
            res.KhuVuc = "central";
        }
        else if (q.Contains("nha trang") || q.Contains("khanh hoa"))
        {
            res.DiemDenCuThe = "nhatrang";
            res.KhuVuc = "central";
        }
        else if (q.Contains("quy nhơn") || q.Contains("quy nhon") || q.Contains("bình định"))
        {
            res.DiemDenCuThe = "quynhon";
            res.KhuVuc = "central";
        }
        else if (q.Contains("huế") || q.Contains("miền trung"))
        {
            res.KhuVuc = "central";
        }
        else if (q.Contains("sa pa") || q.Contains("sapa") || q.Contains("hà nội") || q.Contains("hạ long") || q.Contains("miền bắc"))
        {
            res.KhuVuc = "north";
        }
        else if (q.Contains("miền nam") || q.Contains("nam bộ") || q.Contains("tây nguyên") || q.Contains("vũng tàu"))
        {
            res.KhuVuc = "south";
        }

        // Nếu ngân sách thấp (<= 2.6 triệu) và chưa chọn số đêm, ưu tiên gói 2N1Đ hoặc 3N2Đ tiết kiệm
        if (res.NganSach.HasValue && res.NganSach.Value <= 2_200_000m && !nightMatch.Success)
            res.SoDem = 1;

        return res;
    }

    public async Task<List<GoiYKetQuaDto>> GoiYDuLichAsync(GoiYDuLichRequest req)
    {
        var thang = req.ThangDi is >= 1 and <= 12 ? req.ThangDi.Value : GioVN.Now.Month;
        var soDem = Math.Max(1, req.SoDem);

        var seasons = await db.MuaDuLiches.AsNoTracking().ToListAsync();
        var seasonalAttractions = await db.GoiYdiaDiemTheoMuaAis
            .AsNoTracking()
            .Include(g => g.MaDiaDiemNavigation)
            .Include(g => g.MaMuaNavigation)
            .ToListAsync();
        var tours = await db.GoiYtourAis
            .AsNoTracking()
            .Where(t => t.DangHoatDong)
            .ToListAsync();
        var rooms = await db.LoaiPhongs
            .AsNoTracking()
            .Where(lp => lp.DangHoatDong)
            .OrderBy(lp => lp.GiaCoBan)
            .ToListAsync();

        var results = new List<GoiYKetQuaDto>();

        // 1. Kết hợp từ bảng GoiYTour_AI + MuaDuLich + GoiYDiaDiemTheoMua_AI
        foreach (var tour in tours)
        {
            var branchId = XacDinhChiNhanhTuDiemDen(tour.DiemDen + " " + tour.TieuDe);
            var khuVuc = XacDinhKhuVucTuDiemDen(tour.DiemDen + " " + tour.TieuDe);

            // Xác định khoảng tháng đẹp nhất từ CacThangTotNhat hoặc MuaDuLich
            var (batDau, ketThuc, matchedSeason) = XacDinhMuaCuaTour(tour, seasons, branchId);
            if (matchedSeason != null && !string.IsNullOrWhiteSpace(matchedSeason.KhuVuc) && matchedSeason.KhuVuc != "all")
                khuVuc = matchedSeason.KhuVuc;

            if (!KiemTraKhopKhuVucVaDiemDen(req, khuVuc, branchId, tour.DiemDen + " " + tour.TieuDe))
                continue;

            var laDungMua = ThangNamTrongKhoang(thang, batDau, ketThuc);
            var guList = PhanTichGu(tour.PhongCach, tour.MoTa, tour.TieuDe);
            var sMua = TinhDiemMua(thang, batDau, ketThuc, 0.96m);

            var (kcHcm, phuongTienHcm, phiDiChuyenHcm) = LayThongTinDiChuyenTuHcm(branchId);
            var estMin = tour.GiaToiThieu;
            var estMax = tour.GiaToiDa;
            // Nếu khách xuất phát từ TP.HCM mà đi Đà Nẵng/Hội An (>900km), cộng chi phí vé máy bay thực tế
            if (string.Equals(req.DiemXuatPhat, "hcm", StringComparison.OrdinalIgnoreCase) && branchId == 1 && estMin < 3_500_000m)
            {
                estMin += phiDiChuyenHcm;
                estMax += phiDiChuyenHcm;
            }

            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sGeo = TinhDiemViTriVaDiChuyen(branchId, req.DiemXuatPhat, req.YeuCauGanXuatPhat, req.NganSach);
            var sRating = Math.Clamp((tour.DiemDanhGiaTrungBinh ?? 4.8m) / 5.0m, 0.75m, 1.0m);

            var totalScore = (0.35m * sMua + 0.25m * sBudget + 0.20m * sGeo + 0.15m * sGu + 0.05m * sRating) * 100m;
            if (!laDungMua)
                totalScore -= 18m; // Phạt trực tiếp các tour trái mùa để không bao giờ xếp trên điểm đến đúng mùa

            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, branchId);
            var relatedSpot = seasonalAttractions
                .FirstOrDefault(sa => sa.MaDiaDiemNavigation != null &&
                                      XacDinhChiNhanhTuDiemDen(sa.MaDiaDiemNavigation.Ten + " " + sa.MaDiaDiemNavigation.DiaChi) == branchId &&
                                      ThangNamTrongKhoang(thang, sa.ThangBatDauGoiY, sa.ThangKetThucGoiY))
                ?? seasonalAttractions.FirstOrDefault(sa => sa.MaMua == matchedSeason?.MaId);

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = tour.MaId,
                LoaiGoiY = "Lịch trình & Lưu trú AI đề xuất",
                TieuDe = tour.TieuDe,
                DiemDen = tour.DiemDen,
                KhuVuc = khuVuc,
                TenMuaDepNhat = matchedSeason?.Ten ?? TenMuaTheoChiNhanh(branchId),
                ThoiDiemDepNhat = $"Tháng {batDau} – Tháng {ketThuc}",
                ThoiTietDuKien = relatedSpot?.LoaiThoiTiet ?? matchedSeason?.MoTaThoiTiet ?? MoTaThoiTietTheoChiNhanh(branchId),
                LaDungMua = laDungMua,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = tour.SoNgay,
                DanhSachGu = guList,
                LyDoAiGoiY = tour.MoTa ?? relatedSpot?.LyDoGoiY ?? "Điểm đến lý tưởng theo ngân sách và thời điểm bạn chọn.",
                DiemCheckInNoiBat = relatedSpot?.MaDiaDiemNavigation?.Ten ?? tour.DiemDen,
                DuongDanAnh = tour.DuongDanAnh,
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(totalScore, 45m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? TenPhongMacDinhTheoChiNhanh(branchId),
                GiaPhongMoiDem = bestRoom?.GiaCoBan ?? GiaPhongMacDinhTheoChiNhanh(branchId),
                MaChiNhanh = bestRoom?.MaChiNhanh ?? branchId,
                TenChiNhanh = TenChiNhanhTuId(bestRoom?.MaChiNhanh ?? branchId)
            });
        }

        // 2. Bổ sung các điểm đến từ GoiYDiaDiemTheoMua_AI
        foreach (var sa in seasonalAttractions)
        {
            if (sa.MaDiaDiemNavigation == null || !sa.MaDiaDiemNavigation.DangHoatDong)
                continue;

            var spotText = $"{sa.MaDiaDiemNavigation.Ten} {sa.MaDiaDiemNavigation.DiaChi}";
            var spotBranchId = XacDinhChiNhanhTuDiemDen(spotText);
            var khuVuc = sa.MaMuaNavigation?.KhuVuc ?? XacDinhKhuVucTuDiemDen(spotText);

            if (!KiemTraKhopKhuVucVaDiemDen(req, khuVuc, spotBranchId, spotText))
                continue;

            var laDungMua = ThangNamTrongKhoang(thang, sa.ThangBatDauGoiY, sa.ThangKetThucGoiY);
            var guList = PhanTichGu(sa.MaDiaDiemNavigation.PhongCach, sa.LyDoGoiY, sa.MaDiaDiemNavigation.Ten);
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, spotBranchId);
            var giaPhong = bestRoom?.GiaCoBan ?? GiaPhongMacDinhTheoChiNhanh(spotBranchId);

            var (kcHcm, phuongTienHcm, phiDiChuyenHcm) = LayThongTinDiChuyenTuHcm(spotBranchId);
            var (estMin, estMax) = TinhChiPhiTronGoiThucTe(spotBranchId, giaPhong, soDem, req.DiemXuatPhat, req.NganSach);

            var sMua = TinhDiemMua(thang, sa.ThangBatDauGoiY, sa.ThangKetThucGoiY, sa.DiemPhuHop);
            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sGeo = TinhDiemViTriVaDiChuyen(spotBranchId, req.DiemXuatPhat, req.YeuCauGanXuatPhat, req.NganSach);

            var totalScore = (0.35m * sMua + 0.25m * sBudget + 0.20m * sGeo + 0.15m * sGu + 0.05m * 0.96m) * 100m;
            if (!laDungMua)
                totalScore -= 18m;

            var thoiTiet = sa.LoaiThoiTiet ?? "Nắng đẹp, mát mẻ";
            // Bảo vệ chống dữ liệu cũ ghi nhầm "săn mây" cho Hội An
            if (spotBranchId == 1 && thoiTiet.Contains("săn mây", StringComparison.OrdinalIgnoreCase))
                thoiTiet = "Nắng xuân dịu nhẹ, khô ráo";

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 1000 + sa.MaId,
                LoaiGoiY = "Điểm Check-in Theo Mùa",
                TieuDe = $"Khám phá {sa.MaDiaDiemNavigation.Ten}",
                DiemDen = sa.MaDiaDiemNavigation.DiaChi ?? "Đà Nẵng - Hội An",
                KhuVuc = khuVuc,
                TenMuaDepNhat = sa.MaMuaNavigation?.Ten ?? TenMuaTheoChiNhanh(spotBranchId),
                ThoiDiemDepNhat = $"Tháng {sa.ThangBatDauGoiY} – Tháng {sa.ThangKetThucGoiY}",
                ThoiTietDuKien = thoiTiet,
                LaDungMua = laDungMua,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = soDem + 1,
                DanhSachGu = guList,
                LyDoAiGoiY = sa.LyDoGoiY ?? sa.MaDiaDiemNavigation.MoTa ?? "Điểm check-in nổi bật được AI đánh giá độ phù hợp cao.",
                DiemCheckInNoiBat = sa.MaDiaDiemNavigation.Ten,
                DuongDanAnh = sa.MaDiaDiemNavigation.DuongDanAnh,
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(totalScore, 45m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? TenPhongMacDinhTheoChiNhanh(spotBranchId),
                GiaPhongMoiDem = giaPhong,
                MaChiNhanh = bestRoom?.MaChiNhanh ?? spotBranchId,
                TenChiNhanh = TenChiNhanhTuId(bestRoom?.MaChiNhanh ?? spotBranchId)
            });
        }

        // 3. Đảm bảo luôn có gợi ý chuẩn xác cho Đà Lạt (CN 3) và Phú Quốc (CN 2) kể cả khi chạy In-Memory DB
        BoSungGoiYDaChiNhanhNeuThieu(results, req, rooms, thang, soDem);

        // Nếu có ít nhất 1 gợi ý ĐÚNG MÙA (LaDungMua == true), ưu tiên đưa toàn bộ gợi ý đúng mùa lên đầu và loại bỏ gợi ý lệch mùa quá sâu
        if (results.Any(r => r.LaDungMua))
        {
            var inSeason = results.Where(r => r.LaDungMua).ToList();
            if (inSeason.Count >= 2 || req.ThangDi.HasValue)
                results = inSeason;
        }

        return results
            .OrderByDescending(r => r.LaDungMua)
            .ThenByDescending(r => r.PhanTramPhuHop)
            .ThenBy(r => r.ChiPhiDuKienTu)
            .Take(8)
            .ToList();
    }

    public async Task<ChatAiKetQuaDto> ChatAsync(
        string cauHoi,
        string? maPhienChat = null,
        int? maNguoiDung = null,
        double? viDoKhach = null,
        double? kinhDoKhach = null,
        string? thanhPhoKhach = null)
    {
        var token = string.IsNullOrWhiteSpace(maPhienChat)
            ? $"CHAT-{Guid.NewGuid():N}"[..20]
            : maPhienChat.Trim();

        var session = await db.PhienChatAis.FirstOrDefaultAsync(s => s.MaPhienChat == token);
        if (session == null)
        {
            session = new PhienChatAi
            {
                MaPhienChat = token,
                MaNguoiDung = maNguoiDung,
                TrangThai = "active",
                ThoiGianBatDau = DateTime.UtcNow
            };
            db.PhienChatAis.Add(session);
            await db.SaveChangesAsync();
        }
        else if (session.MaNguoiDung.HasValue && maNguoiDung.HasValue && session.MaNguoiDung != maNguoiDung)
        {
            // Chống chiếm quyền phiên chat của người dùng khác -> cấp phiên mới
            token = $"CHAT-{Guid.NewGuid():N}"[..20];
            session = new PhienChatAi
            {
                MaPhienChat = token,
                MaNguoiDung = maNguoiDung,
                TrangThai = "active",
                ThoiGianBatDau = DateTime.UtcNow
            };
            db.PhienChatAis.Add(session);
            await db.SaveChangesAsync();
        }

        db.TinNhanChatAis.Add(new TinNhanChatAi
        {
            MaPhienChat = session.MaId,
            NguoiGui = "user",
            NoiDungTinNhan = cauHoi,
            LoaiTinNhan = "text",
            NgayTao = DateTime.UtcNow
        });

        var intent = TrichXuatYChinh(cauHoi);
        if (viDoKhach.HasValue && kinhDoKhach.HasValue)
        {
            intent.ViDoXuatPhat = viDoKhach;
            intent.KinhDoXuatPhat = kinhDoKhach;
        }
        if (!string.IsNullOrWhiteSpace(thanhPhoKhach))
        {
            intent.ThanhPhoXuatPhat = thanhPhoKhach;
            intent.DiemXuatPhat = thanhPhoKhach;
        }

        var goiYs = await GoiYDuLichAsync(new GoiYDuLichRequest
        {
            NganSach = intent.NganSach,
            ThangDi = intent.ThangDi,
            PhongCach = intent.PhongCach,
            KhuVuc = intent.KhuVuc,
            DiemXuatPhat = intent.DiemXuatPhat,
            YeuCauGanXuatPhat = intent.YeuCauGanXuatPhat,
            DiemDenCuThe = intent.DiemDenCuThe,
            SoDem = intent.SoDem,
            ViDoXuatPhat = intent.ViDoXuatPhat,
            KinhDoXuatPhat = intent.KinhDoXuatPhat,
            ThanhPhoXuatPhat = intent.ThanhPhoXuatPhat
        });

        var topGoiY = goiYs.Take(3).ToList();
        var (fallbackReply, dbContextSummary) = await XayDungCauTraLoiCoSoAsync(intent, topGoiY);

        string finalReply = fallbackReply;
        string nguon = "Hybrid-Rule-DB";

        var geminiReply = await GoiGeminiNeuCoAsync(cauHoi, dbContextSummary);
        if (!string.IsNullOrWhiteSpace(geminiReply))
        {
            finalReply = geminiReply;
            nguon = "Hybrid-Gemini-RAG";
        }

        var loaiTinNhan = topGoiY.Count > 0 && intent.ChuDe == "goi_y_du_lich" ? "suggestion_card" : "text";
        var duLieuJson = topGoiY.Count > 0 ? JsonSerializer.Serialize(topGoiY) : null;

        db.TinNhanChatAis.Add(new TinNhanChatAi
        {
            MaPhienChat = session.MaId,
            NguoiGui = "ai",
            NoiDungTinNhan = finalReply,
            LoaiTinNhan = loaiTinNhan,
            DuLieuKemTheo = duLieuJson,
            NgayTao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return new ChatAiKetQuaDto
        {
            MaPhienChat = session.MaPhienChat,
            CauTraLoi = finalReply,
            NguonTraLoi = nguon,
            YChinh = intent,
            GoiYKemTheo = topGoiY,
            ThoiGian = DateTime.UtcNow
        };
    }

    private async Task<(string FallbackReply, string ContextSummary)> XayDungCauTraLoiCoSoAsync(
        TrichXuatYChinhResult intent,
        List<GoiYKetQuaDto> topGoiY)
    {
        var now = GioVN.Now;
        var vouchers = await db.MaGiamGia.AsNoTracking()
            .Where(v => v.DangHoatDong && v.HieuLucDen >= now && v.SoLanDaDung < v.GioiHanSuDung)
            .Take(4)
            .Select(v => $"{v.MaCode} (giảm {(StatusMapper.IsPercentDiscount(v.LoaiGiamGia) ? $"{v.GiaTriGiamGia:N0}%" : $"{v.GiaTriGiamGia:N0}đ")})")
            .ToListAsync();

        var loaiPhongs = await db.LoaiPhongs.AsNoTracking()
            .Where(lp => lp.DangHoatDong)
            .OrderBy(lp => lp.GiaCoBan)
            .Select(lp => $"{lp.Ten} ({lp.GiaCoBan:N0}đ/đêm, tối đa {lp.SoNguoiLonToiDa} khách)")
            .ToListAsync();

        if (intent.ChuDe == "voucher")
        {
            var vText = vouchers.Count > 0
                ? $"Hiện hệ thống đang mở các mã ưu đãi: {string.Join(", ", vouchers)}. Bạn có thể nhập mã ngay ở bước Đặt phòng để được trừ trực tiếp!"
                : "Hiện tại bạn có thể kiểm tra mục Ưu Đãi Voucher trên trang chủ để săn mã giảm giá theo mùa.";
            return (vText, $"Voucher khả dụng: {string.Join("; ", vouchers)}");
        }

        if (intent.ChuDe == "gia_phong")
        {
            var rText = loaiPhongs.Count > 0
                ? $"Các hạng phòng đang mở bán gồm: {string.Join(", ", loaiPhongs)}. {(vouchers.Count > 0 ? $"Đặc biệt bạn có thể áp mã {vouchers[0]} khi đặt trực tuyến!" : "")}"
                : "Hệ thống có các hạng phòng từ Standard (550.000đ/đêm) đến Suite hướng biển.";
            return (rText, $"Hạng phòng: {string.Join("; ", loaiPhongs)}");
        }

        if (topGoiY.Count > 0)
        {
            var best = topGoiY[0];
            var budgetInfo = intent.NganSach.HasValue ? $"với ngân sách khoảng {intent.NganSach.Value:N0}đ" : "theo nhu cầu của bạn";
            var monthVal = intent.ThangDi ?? now.Month;
            var monthInfo = intent.ThangDi.HasValue ? $"vào tháng {intent.ThangDi.Value}" : $"trong thời điểm hiện tại (tháng {now.Month})";
            var originInfo = string.Equals(intent.DiemXuatPhat, "hcm", StringComparison.OrdinalIgnoreCase)
                ? (intent.YeuCauGanXuatPhat ? " gần khu vực TP.HCM" : " xuất phát từ TP.HCM")
                : "";

            var seasonExplain = best.LaDungMua
                ? $"Tháng {monthVal} nằm đúng vào mùa đẹp nhất tại đây ({best.ThoiDiemDepNhat} — {best.ThoiTietDuKien})."
                : $"Thời điểm lý tưởng nhất là {best.ThoiDiemDepNhat} ({best.ThoiTietDuKien}).";

            var transportExplain = string.Equals(intent.DiemXuatPhat, "hcm", StringComparison.OrdinalIgnoreCase)
                ? $" Phương tiện gợi ý: {best.PhuongTienGoiY}."
                : "";

            var matchQuality = best.KhoangCachTuHcmKm > 0
                ? $"(Phù hợp nhất: Cách ~{best.KhoangCachTuHcmKm}km • {best.PhanTramPhuHop}% tiêu chí)"
                : $"(Phù hợp nhất: {best.PhanTramPhuHop}% tiêu chí)";

            var reply = $"Gợi ý hàng đầu {budgetInfo}{originInfo} {monthInfo} là **{best.TieuDe}** tại **{best.DiemDen}** {matchQuality}. " +
                        $"{seasonExplain} " +
                        $"{best.LyDoAiGoiY}{transportExplain} " +
                        $"Đề xuất đặt phòng tại chi nhánh **{best.TenChiNhanh}** — hạng phòng **{best.HangPhongPhuHop}** ({best.GiaPhongMoiDem:N0}đ/đêm, dự kiến trọn gói lưu trú + di chuyển + ăn uống từ {best.ChiPhiDuKienTu:N0}đ – {best.ChiPhiDuKienDen:N0}đ).";

            var ctx = string.Join("\n", topGoiY.Select(g =>
                $"- {g.TieuDe} ({g.DiemDen}) | Chi nhánh: {g.TenChiNhanh} | Match {g.PhanTramPhuHop}% | Mùa đẹp: {g.ThoiDiemDepNhat} (Đúng mùa tháng {monthVal}: {g.LaDungMua}) | Thời tiết: {g.ThoiTietDuKien} | Khoảng cách từ TP.HCM: ~{g.KhoangCachTuHcmKm}km ({g.PhuongTienGoiY}) | Chi phí trọn gói khả thi: {g.ChiPhiDuKienTu:N0}-{g.ChiPhiDuKienDen:N0}đ | Hạng phòng: {g.HangPhongPhuHop} ({g.GiaPhongMoiDem:N0}đ/đêm). Lý do: {g.LyDoAiGoiY}"));

            return (reply, ctx);
        }

        return ("Trợ lý AI BookingStay gợi ý bạn khám phá Đà Lạt (mùa săn mây tháng 10 – tháng 3, gần TP.HCM), Phú Quốc (mùa khô biển êm tháng 11 – tháng 4) hoặc Đà Nẵng - Hội An (mùa biển nắng đẹp tháng 2 – tháng 8) cùng hệ thống 3 chi nhánh resort của chúng tôi.", "Điểm đến: Đà Lạt - Phú Quốc - Đà Nẵng");
    }

    private async Task<string?> GoiGeminiNeuCoAsync(string cauHoi, string dbContextSummary)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var client = httpClientFactory.CreateClient();
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

            var prompt = $"""
                Bạn là Trợ lý AI Tư vấn Du lịch & Đặt phòng của hệ thống chuỗi khách sạn BookingStay.AI (gồm 4 chi nhánh: GrandStay Vũng Tàu Oceanfront Hotel, GrandStay Pine Hill Retreat Đà Lạt, GrandStay Sunset Villa Phú Quốc, GrandStay Đà Nẵng Beach Resort).
                Quy tắc bắt buộc:
                1. CHỈ gợi ý điểm đến đứng đầu (Match cao nhất và Đúng mùa = True) trong danh sách dữ liệu CSDL bên dưới.
                2. QUY TẮC BẢO VỆ CHỐNG BỊA (NO-DIGITS RULE): KHÔNG viết bất kỳ chữ số nào (0-9) trong câu dẫn giải thích. Mọi con số (khoảng cách km, nhiệt độ, xác suất mưa, chi phí tiền đồng, ngày tháng) đều do thẻ hệ thống hiển thị từ CSDL.
                3. Viết 2-3 câu cảm xúc, định tính, thân thiện, súc tích bằng tiếng Việt giải thích vì sao điểm đến này đẹp và phù hợp với nhu cầu của khách:
                {dbContextSummary}

                Câu hỏi của khách: "{cauHoi}"
                """;

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(payload);

            var resp = await client.SendAsync(request, cts.Token);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Claim Verifier: Kiểm tra nếu LLM vô tình viết chữ số (dễ sai lệch km, % mưa, giá vé)
            // Nếu phát hiện chữ số -> Fallback về câu trả lời xác định chuẩn từ DB
            if (Regex.IsMatch(text, @"\d"))
            {
                logger.LogInformation("Claim Verifier: LLM vi phạm quy tắc No-Digits (chứa chữ số trong câu dẫn). Chuyển về lời giải thích xác định từ CSDL.");
                return null;
            }

            return text.Trim();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Gemini API fallback sang bộ trả lời chuẩn từ DB.");
            return null;
        }
    }

    public async Task<int> ChayDuBaoNhuCauAsync(int soNgay = 14)
    {
        soNgay = Math.Clamp(soNgay, 1, 30);
        var homNay = DateOnly.FromDateTime(GioVN.HomNay);
        var loaiPhongs = await db.LoaiPhongs.Include(lp => lp.Phongs).Where(lp => lp.DangHoatDong).ToListAsync();
        var seasons = await db.MuaDuLiches.AsNoTracking().ToListAsync();
        var recentBookings = await db.ChiTietDatPhongs
            .AsNoTracking()
            .Include(ct => ct.MaDatPhongNavigation)
            .Where(ct => ct.MaDatPhongNavigation.TrangThai != "Cancelled")
            .ToListAsync();

        var countCreated = 0;

        for (var i = 1; i <= soNgay; i++)
        {
            var ngay = homNay.AddDays(i);
            var dtNgay = ngay.ToDateTime(TimeOnly.MinValue);
            var isPeakSeason = seasons.Any(s => s.LaMuaCaoDiem && ThangNamTrongKhoang(ngay.Month, s.ThangBatDau, s.ThangKetThuc));
            var isWeekend = ngay.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
            var seasonMultiplier = (isPeakSeason ? 1.25m : 0.95m) * (isWeekend ? 1.20m : 1.0m);

            foreach (var lp in loaiPhongs)
            {
                var totalRooms = Math.Max(1, lp.Phongs.Count);
                var confirmedOnDate = recentBookings.Count(ct =>
                    ct.MaLoaiPhong == lp.MaId &&
                    ct.NgayNhanPhongDuKien.Date <= dtNgay &&
                    ct.NgayTraPhongDuKien.Date > dtNgay);

                var historicalSameDayOfWeek = recentBookings.Count(ct =>
                    ct.MaLoaiPhong == lp.MaId &&
                    ct.NgayNhanPhongDuKien.DayOfWeek == dtNgay.DayOfWeek);

                var baseline = Math.Max(1m, historicalSameDayOfWeek / 4.0m);
                var predictedBookings = Math.Max(confirmedOnDate, (int)Math.Round(baseline * seasonMultiplier));
                var occupancyPct = Math.Clamp(Math.Round((decimal)predictedBookings / totalRooms * 100m, 2), 0m, 100m);

                var existing = await db.DuBaoNhuCauDatPhongAis
                    .FirstOrDefaultAsync(d => d.NgayDuBao == ngay && d.MaLoaiPhong == lp.MaId);

                var note = $"Dự báo thống kê ({(isPeakSeason ? "Mùa cao điểm" : "Mùa tiêu chuẩn")}{(isWeekend ? ", Cuối tuần" : "")}) — Đã đặt trước: {confirmedOnDate}/{totalRooms} phòng.";

                if (existing == null)
                {
                    db.DuBaoNhuCauDatPhongAis.Add(new DuBaoNhuCauDatPhongAi
                    {
                        NgayDuBao = ngay,
                        MaLoaiPhong = lp.MaId,
                        SoDatPhongDuBao = predictedBookings,
                        TyLeLapDayDuBao = occupancyPct,
                        PhienBanMoHinh = "thongke-v1",
                        ThoiGianDuDoan = DateTime.UtcNow,
                        GhiChuDuBao = note
                    });
                }
                else
                {
                    existing.SoDatPhongDuBao = predictedBookings;
                    existing.TyLeLapDayDuBao = occupancyPct;
                    existing.PhienBanMoHinh = "thongke-v1";
                    existing.ThoiGianDuDoan = DateTime.UtcNow;
                    existing.GhiChuDuBao = note;
                }
                countCreated++;
            }
        }

        await db.SaveChangesAsync();
        return countCreated;
    }

    public async Task<int> ChayDuDoanHuyPhongAsync()
    {
        var bookings = await db.DatPhongs
            .Include(d => d.ChiTietDatPhongs)
            .Include(d => d.HoaDon)
            .Where(d => d.TrangThai == "Pending" || d.TrangThai == "Confirmed" || d.TrangThai == "Cancelled" || d.TrangThai == "CheckedIn")
            .OrderByDescending(d => d.NgayTao)
            .Take(50)
            .ToListAsync();

        var cancelledUserIds = await db.DatPhongs
            .Where(d => d.TrangThai == "Cancelled" && d.MaNguoiDung != null)
            .Select(d => d.MaNguoiDung!.Value)
            .Distinct()
            .ToListAsync();
        var cancelledSet = cancelledUserIds.ToHashSet();

        var updatedCount = 0;

        foreach (var b in bookings)
        {
            var score = 10m;
            var reasons = new List<string>();

            var hasDepositOrPaid = b.TienDatCoc > 0 || (b.HoaDon != null && b.HoaDon.TrangThai == "Paid");
            if (!hasDepositOrPaid)
            {
                score += 30m;
                reasons.Add("Chưa đặt cọc hoặc thanh toán trước (+30%)");
            }

            var earliestCheckIn = b.ChiTietDatPhongs.OrderBy(c => c.NgayNhanPhongDuKien).FirstOrDefault()?.NgayNhanPhongDuKien;
            if (earliestCheckIn.HasValue && (earliestCheckIn.Value - b.NgayTao).TotalDays > 14)
            {
                score += 15m;
                reasons.Add("Đặt xa ngày nhận phòng > 14 ngày (+15%)");
            }

            if (string.Equals(b.KenhDatPhong, "ota", StringComparison.OrdinalIgnoreCase))
            {
                score += 15m;
                reasons.Add("Đặt qua kênh OTA dễ thay đổi lịch trình (+15%)");
            }

            if (b.MaNguoiDung.HasValue && cancelledSet.Contains(b.MaNguoiDung.Value))
            {
                score += 20m;
                reasons.Add("Khách hàng từng có lịch sử hủy phòng (+20%)");
            }

            if (!b.MaNguoiDung.HasValue)
            {
                score += 10m;
                reasons.Add("Khách vãng lai chưa liên kết tài khoản (+10%)");
            }

            if (b.TongTien >= 5_000_000m)
            {
                score += 10m;
                reasons.Add("Giá trị đơn cao trên 5 triệu (+10%)");
            }

            var prob = Math.Clamp(score, 5m, 95m);
            var riskLevel = prob >= 60m ? "high" : prob >= 35m ? "medium" : "low";
            var actualCancelled = b.TrangThai == "Cancelled" ? true : b.TrangThai == "CheckedIn" || b.TrangThai == "CheckedOut" ? false : (bool?)null;

            var existing = await db.DuDoanHuyPhongAis.FirstOrDefaultAsync(x => x.MaDatPhong == b.MaId);
            var mainReason = reasons.Count > 0 ? string.Join("; ", reasons) : "Đơn đã đặt cọc và xác nhận ổn định";

            if (existing == null)
            {
                db.DuDoanHuyPhongAis.Add(new DuDoanHuyPhongAi
                {
                    MaDatPhong = b.MaId,
                    XacSuatHuyPhong = prob,
                    MucDoRuiRo = riskLevel,
                    YeuToChinh = mainReason,
                    ThoiGianDuDoan = DateTime.UtcNow,
                    ThucTeDaHuy = actualCancelled,
                    PhienBanMoHinh = "risk-rule-v1"
                });
            }
            else
            {
                existing.XacSuatHuyPhong = prob;
                existing.MucDoRuiRo = riskLevel;
                existing.YeuToChinh = mainReason;
                existing.ThoiGianDuDoan = DateTime.UtcNow;
                existing.ThucTeDaHuy = actualCancelled;
                existing.PhienBanMoHinh = "risk-rule-v1";
            }

            updatedCount++;
        }

        await db.SaveChangesAsync();
        return updatedCount;
    }

    public static int XacDinhChiNhanhTuDiemDen(string? diemDen)
    {
        if (string.IsNullOrWhiteSpace(diemDen)) return 1;
        var d = diemDen.ToLowerInvariant();
        if (d.Contains("củ chi") || d.Contains("cu chi"))
            return 5;
        if (d.Contains("hải phòng") || d.Contains("hai phong"))
            return 6;
        if (d.Contains("vũng tàu") || d.Contains("vung tau") || d.Contains("bà rịa") || d.Contains("thùy vân") || d.Contains("nghinh phong") || d.Contains("hồ tràm") || d.Contains("bãi sau"))
            return 4;
        if (d.Contains("phú quốc") || d.Contains("phu quoc") || d.Contains("kiên giang") || d.Contains("nam bộ") || d.Contains("bãi trường") || d.Contains("sunset town"))
            return 2;
        if (d.Contains("đà lạt") || d.Contains("da lat") || d.Contains("tây nguyên") || d.Contains("lâm đồng") || d.Contains("cầu đất") || d.Contains("tuyền lâm") || d.Contains("pine hill"))
            return 3;
        return 1; // Đà Nẵng / Miền Trung / Trụ sở chính
    }

    public static string TenChiNhanhTuId(int branchId) => branchId switch
    {
        6 => "HẢI PHÒNG Hotel",
        5 => "CUCHI Hotel",
        4 => "GrandStay Vũng Tàu Oceanfront Hotel",
        2 => "GrandStay Sunset Villa Phú Quốc",
        3 => "GrandStay Pine Hill Retreat Đà Lạt",
        _ => "GrandStay Đà Nẵng Beach Resort"
    };

    private static string TenMuaTheoChiNhanh(int branchId) => branchId switch
    {
        6 => "Mùa Thu - Đông Thành Phố Cảng Hải Phòng",
        5 => "Mùa Khô Nắng Đẹp & Trải Nghiệm Sinh Thái Củ Chi",
        4 => "Mùa Khô Nắng Ấm & Biển Êm Vũng Tàu",
        3 => "Mùa Khô Săn Mây & Hoa Cuối Năm Đà Lạt",
        2 => "Mùa Khô Biển Đảo Phú Quốc",
        _ => "Mùa Biển Đẹp & Nắng Vàng Miền Trung"
    };

    private static string MoTaThoiTietTheoChiNhanh(int branchId) => branchId switch
    {
        6 => "Thời tiết se lạnh 18-24°C, khô ráo, gió mát",
        5 => "Nắng ấm chan hòa 28-32°C, râm mát nhiều cây xanh",
        4 => "Nắng ấm chan hòa 28-31°C, gió biển mát lành, biển êm",
        3 => "Se lạnh 15-20°C, nắng trong, biển mây buổi sớm",
        2 => "Nắng ấm 28°C, khô ráo, biển êm xanh ngắt",
        _ => "Nắng đẹp, ít mưa, biển êm"
    };

    private static string TenPhongMacDinhTheoChiNhanh(int branchId) => branchId switch
    {
        6 => "Phòng Deluxe City View Hải Phòng",
        5 => "Phòng Standard Tiện Nghi Củ Chi",
        4 => "GrandStay Superior Ocean View Vũng Tàu",
        3 => "Pine Hill Retreat Superior",
        2 => "GrandStay Suite Family Resort",
        _ => "GrandStay Deluxe Sea View"
    };

    private static decimal GiaPhongMacDinhTheoChiNhanh(int branchId) => branchId switch
    {
        6 => 750_000m,
        5 => 500_000m,
        4 => 650_000m,
        3 => 750_000m,
        2 => 1_500_000m,
        _ => 850_000m
    };

    private static (decimal EstMin, decimal EstMax) TinhChiPhiTronGoiThucTe(
        int branchId,
        decimal giaPhongMoiDem,
        int soDem,
        string? diemXuatPhat,
        decimal? nganSach)
    {
        if (branchId == 5) // Củ Chi (~45km từ TP.HCM, chi phí cực rẻ)
        {
            var effectiveNights = Math.Min(soDem, 1);
            var roomCost = giaPhongMoiDem * effectiveNights;
            var estMin = roomCost + 150_000m; // Xăng xe máy / bus + vé tham quan địa đạo 35k
            var estMax = roomCost + 450_000m; // Xe hơi / taxi + ăn uống đặc sản bò tơ Củ Chi
            return (estMin, estMax);
        }

        if (branchId == 4) // Vũng Tàu (Cách TP.HCM ~105km, xe khách khứ hồi ~350k)
        {
            var effectiveNights = nganSach.HasValue && nganSach.Value <= 2_000_000m ? 1 : Math.Min(soDem, 2);
            var roomCost = giaPhongMoiDem * effectiveNights;
            var estMin = roomCost + 400_000m; // Xe khứ hồi + hải sản nhẹ
            var estMax = roomCost + 950_000m; // Xe Hoa Mai Limousine + ẩm thực hải sản Bãi Sau
            return (estMin, estMax);
        }

        if (branchId == 3) // Đà Lạt (Gần TP.HCM ~300km, đi xe Limousine khứ hồi ~650k)
        {
            var effectiveNights = nganSach.HasValue && nganSach.Value <= 2_600_000m ? 1 : Math.Min(soDem, 2);
            var roomCost = giaPhongMoiDem * effectiveNights;
            var estMin = roomCost + 600_000m; // Xe khứ hồi + ăn nhẹ
            var estMax = roomCost + 1_350_000m; // Xe Limousine + ăn uống + cafe săn mây
            return (estMin, estMax);
        }

        if (branchId == 2) // Phú Quốc (Resort 5 sao + vé tàu/máy bay ra đảo)
        {
            var roomCost = giaPhongMoiDem * Math.Max(1, soDem);
            return (Math.Max(3_500_000m, roomCost + 1_200_000m), Math.Max(5_200_000m, roomCost + 2_400_000m));
        }

        // Đà Nẵng / Hội An (Chi nhánh 1)
        var baseRoomCost = giaPhongMoiDem * Math.Max(1, soDem);
        if (string.Equals(diemXuatPhat, "hcm", StringComparison.OrdinalIgnoreCase))
        {
            // Nếu đi từ TP.HCM ra Đà Nẵng/Hội An (>900km) phải cộng vé máy bay khứ hồi (~2.2 triệu)
            return (baseRoomCost + 2_200_000m, baseRoomCost + 3_500_000m);
        }

        return (baseRoomCost + 500_000m, baseRoomCost + 1_500_000m);
    }

    private static (int BatDau, int KetThuc, MuaDuLich? MatchedSeason) XacDinhMuaCuaTour(
        GoiYtourAi tour,
        List<MuaDuLich> seasons,
        int branchId)
    {
        if (!string.IsNullOrWhiteSpace(tour.CacThangTotNhat))
        {
            try
            {
                var months = JsonSerializer.Deserialize<List<int>>(tour.CacThangTotNhat);
                if (months is { Count: >= 2 })
                {
                    var matched = seasons.FirstOrDefault(s =>
                        string.Equals(s.LoaiMua, tour.MuaDuLich, StringComparison.OrdinalIgnoreCase));
                    return (months.First(), months.Last(), matched);
                }
            }
            catch { }
        }

        var season = seasons.FirstOrDefault(s =>
            string.Equals(s.LoaiMua, tour.MuaDuLich, StringComparison.OrdinalIgnoreCase));

        if (season != null)
            return (season.ThangBatDau, season.ThangKetThuc, season);

        return branchId switch
        {
            3 => (10, 3, null), // Đà Lạt mùa khô tháng 10 -> tháng 3
            2 => (11, 4, null), // Phú Quốc mùa khô tháng 11 -> tháng 4
            _ => (3, 8, null)   // Đà Nẵng - Hội An tháng 3 -> tháng 8
        };
    }

    private static bool KiemTraKhopKhuVucVaDiemDen(GoiYDuLichRequest req, string khuVuc, int branchId, string text)
    {
        if (!string.IsNullOrWhiteSpace(req.DiemDenCuThe))
        {
            return req.DiemDenCuThe switch
            {
                "cuchi" => branchId == 5,
                "haiphong" => branchId == 6,
                "vungtau" => branchId == 4,
                "dalat" => branchId == 3,
                "phuquoc" => branchId == 2,
                "danang" or "hoian" => branchId == 1,
                _ => true
            };
        }

        // Khi khách yêu cầu gần khu vực TP.HCM: loại trừ ngay các điểm miền Trung / miền Bắc cách >900km
        if (string.Equals(req.DiemXuatPhat, "hcm", StringComparison.OrdinalIgnoreCase) && req.YeuCauGanXuatPhat)
        {
            return branchId == 5 || branchId == 4 || branchId == 3 || branchId == 2;
        }

        if (!string.IsNullOrWhiteSpace(req.KhuVuc) && !req.KhuVuc.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(khuVuc, req.KhuVuc, StringComparison.OrdinalIgnoreCase) ||
                   text.Contains(req.KhuVuc, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private static void BoSungGoiYDaChiNhanhNeuThieu(
        List<GoiYKetQuaDto> results,
        GoiYDuLichRequest req,
        List<LoaiPhong> rooms,
        int thang,
        int soDem)
    {
        // 0. Đảm bảo Chi nhánh 5 (CUCHI Hotel - Sát vách TP.HCM) luôn được đưa vào đầu danh sách nếu khách hỏi Củ Chi
        if ((req.DiemDenCuThe == "cuchi" || (!string.IsNullOrEmpty(req.DiemDenCuThe) && req.DiemDenCuThe.Contains("cuchi"))) &&
            !results.Any(r => r.MaChiNhanh == 5))
        {
            var guList = new List<string> { "phieuluu", "van_hoa", "thugian" };
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, 5);
            var giaPhong = bestRoom?.GiaCoBan ?? 500_000m;
            var (estMin, estMax) = TinhChiPhiTronGoiThucTe(5, giaPhong, soDem, req.DiemXuatPhat, req.NganSach);
            var (kcHcm, phuongTienHcm, _) = LayThongTinDiChuyenTuHcm(5);

            results.Insert(0, new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 111,
                LoaiGoiY = "Trải Nghiệm Dã Ngoại & Điểm Đến Gần TP.HCM",
                TieuDe = "Khám Phá Địa Đạo Củ Chi & Nghỉ Dưỡng Thư Thái (Gần TP.HCM)",
                DiemDen = "Huyện Củ Chi, TP. Hồ Chí Minh (Cách trung tâm ~45km)",
                KhuVuc = "south",
                TenMuaDepNhat = "Mùa Khô Nắng Đẹp Phương Nam",
                ThoiDiemDepNhat = "Tháng 11 – Tháng 4",
                ThoiTietDuKien = "Nắng ấm chan hòa 28-32°C, râm mát nhiều cây xanh",
                LaDungMua = true,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = Math.Min(2, soDem),
                DanhSachGu = guList,
                LyDoAiGoiY = "Lựa chọn số 1 khi muốn đi dã ngoại đổi gió sát vách TP.HCM (chỉ 1h di chuyển), chi phí cực rẻ, tham quan kỳ quan địa đạo lịch sử và lưu trú tại CUCHI Hotel.",
                DiemCheckInNoiBat = "Địa đạo Bến Dược & Vườn sinh thái ven sông",
                DuongDanAnh = "https://images.unsplash.com/photo-1544644181-1484b3fdfc62?auto=format&fit=crop&w=800&q=80",
                PhanTramPhuHop = 96,
                HangPhongPhuHop = bestRoom?.Ten ?? "Phòng Standard CUCHI",
                GiaPhongMoiDem = giaPhong,
                MaChiNhanh = 5,
                TenChiNhanh = TenChiNhanhTuId(5),
                DiemBienTap = 4.9m
            });
        }

        // Đảm bảo Chi nhánh 4 (Vũng Tàu - Sát vách TP.HCM ~105km) luôn có mặt nếu khách tìm gần TP.HCM
        if ((req.YeuCauGanXuatPhat || string.Equals(req.DiemXuatPhat, "hcm", StringComparison.OrdinalIgnoreCase)) &&
            KiemTraKhopKhuVucVaDiemDen(req, "south", 4, "Vũng Tàu Bà Rịa") &&
            !results.Any(r => r.MaChiNhanh == 4))
        {
            var laDungMua = ThangNamTrongKhoang(thang, 11, 4);
            var guList = new List<string> { "songao", "thugian", "check_in" };
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, 4);
            var giaPhong = bestRoom?.GiaCoBan ?? 650_000m;
            var (estMin, estMax) = TinhChiPhiTronGoiThucTe(4, giaPhong, soDem, req.DiemXuatPhat, req.NganSach);
            var (kcHcm, phuongTienHcm, _) = LayThongTinDiChuyenTuHcm(4);

            var sMua = TinhDiemMua(thang, 11, 4, 0.98m);
            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sGeo = TinhDiemViTriVaDiChuyen(4, req.DiemXuatPhat, req.YeuCauGanXuatPhat, req.NganSach);
            var score = (0.35m * sMua + 0.25m * sBudget + 0.20m * sGeo + 0.15m * sGu + 0.05m * 0.98m) * 100m;
            if (!laDungMua) score -= 18m;

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 101,
                LoaiGoiY = "Nghỉ Dưỡng Biển & Điểm Đến Gần TP.HCM",
                TieuDe = "Kỳ Nghỉ Đổi Gió Biển Vũng Tàu & Check-in Mũi Nghinh Phong (Gần TP.HCM)",
                DiemDen = "TP. Vũng Tàu, Bà Rịa - Vũng Tàu (Cách TP.HCM ~105km)",
                KhuVuc = "south",
                TenMuaDepNhat = "Mùa Khô Nắng Ấm & Biển Êm Vũng Tàu",
                ThoiDiemDepNhat = "Tháng 11 – Tháng 4",
                ThoiTietDuKien = "Nắng ấm chan hòa 28-31°C, gió biển mát lành, biển êm",
                LaDungMua = laDungMua,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = Math.Min(3, soDem + 1),
                DanhSachGu = guList,
                LyDoAiGoiY = "Lựa chọn số 1 khi muốn đi đổi gió sát vách TP.HCM (chỉ 2h đi xe), chi phí di chuyển cực rẻ, bờ biển Bãi Sau sóng êm và khách sạn GrandStay Vũng Tàu tọa lạc ngay mặt tiền đường Thùy Vân.",
                DiemCheckInNoiBat = "Bãi Sau Thùy Vân & Mũi Nghinh Phong",
                DuongDanAnh = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=800&q=80",
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(score + 10m, 50m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? "GrandStay Superior Ocean View Vũng Tàu",
                GiaPhongMoiDem = giaPhong,
                MaChiNhanh = 4,
                TenChiNhanh = TenChiNhanhTuId(4),
                DiemBienTap = 4.8m
            });
        }

        // Đảm bảo Chi nhánh 3 (Đà Lạt - Gần TP.HCM) luôn có mặt nếu khớp bộ lọc
        if (KiemTraKhopKhuVucVaDiemDen(req, "south", 3, "Đà Lạt Lâm Đồng") && !results.Any(r => r.MaChiNhanh == 3))
        {
            var laDungMua = ThangNamTrongKhoang(thang, 10, 3);
            var guList = new List<string> { "songao", "thugian", "phieuluu" };
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, 3);
            var giaPhong = bestRoom?.GiaCoBan ?? 750_000m;
            var (estMin, estMax) = TinhChiPhiTronGoiThucTe(3, giaPhong, soDem, req.DiemXuatPhat, req.NganSach);
            var (kcHcm, phuongTienHcm, _) = LayThongTinDiChuyenTuHcm(3);

            var sMua = TinhDiemMua(thang, 10, 3, 0.99m);
            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sGeo = TinhDiemViTriVaDiChuyen(3, req.DiemXuatPhat, req.YeuCauGanXuatPhat, req.NganSach);
            var score = (0.35m * sMua + 0.25m * sBudget + 0.20m * sGeo + 0.15m * sGu + 0.05m * 0.98m) * 100m;
            if (!laDungMua) score -= 18m;

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 3001,
                LoaiGoiY = "Điểm Check-in & Lưu Trú Gần TP.HCM",
                TieuDe = "Khám phá Đồi Chè Cầu Đất & Thung Lũng Săn Mây Đà Lạt",
                DiemDen = "TP. Đà Lạt, Tỉnh Lâm Đồng (Cách TP.HCM ~300km)",
                KhuVuc = "south",
                TenMuaDepNhat = "Mùa Khô Săn Mây & Hoa Cuối Năm Đà Lạt",
                ThoiDiemDepNhat = "Tháng 10 – Tháng 3",
                ThoiTietDuKien = "Se lạnh 15-20°C, nắng trong, biển mây buổi sớm",
                LaDungMua = laDungMua,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = soDem + 1,
                DanhSachGu = guList,
                LyDoAiGoiY = "Cách TP.HCM chỉ ~300km (6 tiếng đi xe Limousine, tiết kiệm tối đa chi phí di chuyển). Tháng 10 đến tháng 3 là đỉnh cao mùa khô Đà Lạt: trời nắng trong veo, se lạnh 15-20°C, biển mây dày đặc và đồi cỏ hồng/hoa cuối năm nở rộ ở mọi góc chụp ảnh sống ảo.",
                DiemCheckInNoiBat = "Đồi Chè Cầu Đất, Hồ Tuyền Lâm, Đồi Cỏ Hồng",
                DuongDanAnh = "https://images.unsplash.com/photo-1583417319070-4a69db38a482?auto=format&fit=crop&w=800&q=80",
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(score, 45m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? "Pine Hill Retreat Superior",
                GiaPhongMoiDem = giaPhong,
                MaChiNhanh = 3,
                TenChiNhanh = TenChiNhanhTuId(3)
            });
        }

        // Đảm bảo Chi nhánh 2 (Phú Quốc - Miền Nam) luôn có mặt nếu khớp bộ lọc
        if (KiemTraKhopKhuVucVaDiemDen(req, "south", 2, "Phú Quốc Kiên Giang") && !results.Any(r => r.MaChiNhanh == 2))
        {
            var laDungMua = ThangNamTrongKhoang(thang, 11, 4);
            var guList = new List<string> { "songao", "thugian", "giadinh" };
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, 2);
            var giaPhong = bestRoom?.GiaCoBan ?? 1_500_000m;
            var (estMin, estMax) = TinhChiPhiTronGoiThucTe(2, giaPhong, soDem, req.DiemXuatPhat, req.NganSach);
            var (kcHcm, phuongTienHcm, _) = LayThongTinDiChuyenTuHcm(2);

            var sMua = TinhDiemMua(thang, 11, 4, 0.97m);
            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sGeo = TinhDiemViTriVaDiChuyen(2, req.DiemXuatPhat, req.YeuCauGanXuatPhat, req.NganSach);
            var score = (0.35m * sMua + 0.25m * sBudget + 0.20m * sGeo + 0.15m * sGu + 0.05m * 0.96m) * 100m;
            if (!laDungMua) score -= 18m;

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 2001,
                LoaiGoiY = "Nghỉ Dưỡng Biển Đảo Phương Nam",
                TieuDe = "Khám phá Thị Trấn Hoàng Hôn (Sunset Town) & Bãi Trường Phú Quốc",
                DiemDen = "Bãi Trường & An Thới, TP. Phú Quốc, Tỉnh Kiên Giang",
                KhuVuc = "south",
                TenMuaDepNhat = "Mùa Khô Biển Đảo Phú Quốc",
                ThoiDiemDepNhat = "Tháng 11 – Tháng 4",
                ThoiTietDuKien = "Nắng ấm 28°C, khô ráo, biển êm xanh ngắt",
                LaDungMua = laDungMua,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = soDem + 1,
                DanhSachGu = guList,
                LyDoAiGoiY = "Tháng 11 đến tháng 4 là mùa khô đẹp nhất tại đảo ngọc Phú Quốc. Biển êm, nắng vàng rực rỡ, kiến trúc Địa Trung Hải và hoàng hôn Bãi Trường lên hình sống ảo tuyệt đẹp.",
                DiemCheckInNoiBat = "Sunset Town, Cầu Hôn, Bãi Trường",
                DuongDanAnh = "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=800&q=80",
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(score, 45m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? "GrandStay Suite Family Resort",
                GiaPhongMoiDem = giaPhong,
                MaChiNhanh = 2,
                TenChiNhanh = TenChiNhanhTuId(2)
            });
        }

        // Đảm bảo Chi nhánh 5 (CUCHI Hotel - Miền Nam) luôn có mặt nếu khớp bộ lọc hoặc khách hỏi Củ Chi
        if (KiemTraKhopKhuVucVaDiemDen(req, "south", 5, "Củ Chi TP. Hồ Chí Minh") && !results.Any(r => r.MaChiNhanh == 5))
        {
            var laDungMua = true; // Củ Chi quanh năm tham quan tốt
            var guList = new List<string> { "vanhoa", "thugian", "giadinh", "amthuc" };
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, 5);
            var giaPhong = bestRoom?.GiaCoBan ?? 450_000m;
            var (estMin, estMax) = TinhChiPhiTronGoiThucTe(5, giaPhong, soDem, req.DiemXuatPhat, req.NganSach);
            var (kcHcm, phuongTienHcm, _) = LayThongTinDiChuyenTuHcm(5);

            var sMua = 0.95m;
            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sGeo = TinhDiemViTriVaDiChuyen(5, req.DiemXuatPhat, req.YeuCauGanXuatPhat, req.NganSach);
            var score = (0.35m * sMua + 0.25m * sBudget + 0.20m * sGeo + 0.15m * sGu + 0.05m * 0.98m) * 100m;

            // Nếu người dùng hỏi đích danh Củ Chi thì ưu tiên điểm số cao nhất (96-98%)
            if (req.DiemDenCuThe == "cuchi")
            {
                score = Math.Max(score, 96m);
            }

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 5001,
                LoaiGoiY = "Dã Ngoại Lịch Sử & Sinh Thái Ngoại Thành",
                TieuDe = "Khám phá Địa đạo Củ Chi & Làng sinh thái ngoại thành TP.HCM",
                DiemDen = "An Nhơn Tây, Huyện Củ Chi, TP. Hồ Chí Minh",
                KhuVuc = "south",
                TenMuaDepNhat = "Mùa Khô Khám Phá Địa Đạo",
                ThoiDiemDepNhat = "Tháng 11 – Tháng 4",
                ThoiTietDuKien = "Nắng ráo 27-31°C, bóng cây râm mát, khô ráo",
                LaDungMua = laDungMua,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = Math.Max(1, soDem + 1),
                DanhSachGu = guList,
                LyDoAiGoiY = "Cách trung tâm TP.HCM chỉ ~45km (khoảng 1 tiếng chạy xe máy hoặc xe buýt/ô tô), vô cùng thuận tiện cho chuyến đi trong ngày hoặc nghỉ ngơi cuối tuần. Tham quan hệ thống Địa đạo Bến Dược - Bến Đình, thưởng thức khoai mì chấm muối mè và không gian xanh thoáng đãng tại Củ Chi.",
                DiemCheckInNoiBat = "Địa đạo Củ Chi (Bến Dược & Bến Đình), Vườn cây ăn trái Trung An",
                DuongDanAnh = "https://images.unsplash.com/photo-1578632767115-351597cf2477?auto=format&fit=crop&w=800&q=80",
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(score, 45m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? "Phòng Nghỉ Sinh Thái Củ Chi Eco",
                GiaPhongMoiDem = giaPhong,
                MaChiNhanh = 5,
                TenChiNhanh = TenChiNhanhTuId(5)
            });
        }

        // Đảm bảo Chi nhánh 6 (HẢI PHÒNG Hotel - Miền Bắc) luôn có mặt nếu khớp bộ lọc hoặc khách hỏi Hải Phòng
        if (KiemTraKhopKhuVucVaDiemDen(req, "north", 6, "Hải Phòng Cát Bà Đồ Sơn") && !results.Any(r => r.MaChiNhanh == 6))
        {
            var laDungMua = ThangNamTrongKhoang(thang, 4, 10);
            var guList = new List<string> { "bien", "amthuc", "songao" };
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach, 6);
            var giaPhong = bestRoom?.GiaCoBan ?? 900_000m;
            var (estMin, estMax) = TinhChiPhiTronGoiThucTe(6, giaPhong, soDem, req.DiemXuatPhat, req.NganSach);
            var (kcHcm, phuongTienHcm, _) = LayThongTinDiChuyenTuHcm(6);

            var sMua = TinhDiemMua(thang, 4, 10, 0.95m);
            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sGeo = TinhDiemViTriVaDiChuyen(6, req.DiemXuatPhat, req.YeuCauGanXuatPhat, req.NganSach);
            var score = (0.35m * sMua + 0.25m * sBudget + 0.20m * sGeo + 0.15m * sGu + 0.05m * 0.95m) * 100m;
            if (!laDungMua) score -= 15m;

            if (req.DiemDenCuThe == "haiphong")
            {
                score = Math.Max(score, 96m);
            }

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 6001,
                LoaiGoiY = "Food Tour Thành Phố Cảng & Biển Đảo",
                TieuDe = "Food Tour Đất Cảng Hải Phòng & Vịnh Lan Hạ Đảo Cát Bà",
                DiemDen = "TP. Hải Phòng & Quần đảo Cát Bà",
                KhuVuc = "north",
                TenMuaDepNhat = "Mùa Biển Đảo & Food Tour",
                ThoiDiemDepNhat = "Tháng 4 – Tháng 10",
                ThoiTietDuKien = "Nắng ấm 28-32°C, gió biển trong lành",
                LaDungMua = laDungMua,
                KhoangCachTuHcmKm = kcHcm,
                PhuongTienGoiY = phuongTienHcm,
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = soDem + 1,
                DanhSachGu = guList,
                LyDoAiGoiY = "Thành phố Cảng Hải Phòng nổi tiếng với văn hóa ẩm thực độc đáo (Bánh đa cua, Bánh mì que, Nem cua bể) và cảnh quan tuyệt mỹ vịnh Lan Hạ, đảo Cát Bà.",
                DiemCheckInNoiBat = "Vịnh Lan Hạ, Đảo Cát Bà, Nhà hát lớn Hải Phòng",
                DuongDanAnh = "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=800&q=80",
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(score, 45m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? "Deluxe Port City View",
                GiaPhongMoiDem = giaPhong,
                MaChiNhanh = 6,
                TenChiNhanh = TenChiNhanhTuId(6)
            });
        }
    }

    private static LoaiPhong? ChonPhongPhuHop(List<LoaiPhong> rooms, decimal? nganSach, int soDem, int soKhach, int? targetBranchId = null)
    {
        if (rooms.Count == 0) return null;

        var pool = rooms;
        if (targetBranchId.HasValue)
        {
            var branchSpecific = rooms.Where(r => r.MaChiNhanh == targetBranchId.Value).ToList();
            if (branchSpecific.Count > 0)
                pool = branchSpecific;
            else
                return null; // Nếu DB chưa có phòng của chi nhánh này thì để fallback về tên/giá chuẩn của chi nhánh đó, KHÔNG lấy nhầm phòng của chi nhánh khác!
        }

        var suitableByGuest = pool.Where(r => r.SoNguoiLonToiDa >= soKhach).ToList();
        var candidatePool = suitableByGuest.Count > 0 ? suitableByGuest : pool;

        if (!nganSach.HasValue || nganSach.Value <= 0)
            return candidatePool.First();

        // Dành tối đa ~65% tổng ngân sách cho lưu trú
        var maxRoomBudgetPerNight = nganSach.Value * 0.65m / Math.Max(1, soDem);
        var affordable = candidatePool.Where(r => r.GiaCoBan <= maxRoomBudgetPerNight).OrderByDescending(r => r.GiaCoBan).FirstOrDefault();
        return affordable ?? candidatePool.OrderBy(r => r.GiaCoBan).First();
    }

    private static string XacDinhKhuVucTuDiemDen(string diemDen)
    {
        var d = diemDen.ToLowerInvariant();
        if (d.Contains("sa pa") || d.Contains("hà nội") || d.Contains("hạ long") || d.Contains("ninh bình") || d.Contains("hải phòng") || d.Contains("hai phong"))
            return "north";
        if (d.Contains("phú quốc") || d.Contains("phu quoc") || d.Contains("kiên giang") || d.Contains("đà lạt") || d.Contains("da lat") || d.Contains("lâm đồng") || d.Contains("côn đảo") || d.Contains("hồ chí minh") || d.Contains("củ chi") || d.Contains("cu chi") || d.Contains("vũng tàu") || d.Contains("vung tau"))
            return "south";
        return "central";
    }
}
