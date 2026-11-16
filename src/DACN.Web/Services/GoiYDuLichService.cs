using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DACN.Infrastructure.Data;
using DACN.Web.Common;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Services;

public class GoiYDuLichRequest
{
    public decimal? NganSach { get; set; }
    public int? ThangDi { get; set; }
    public string? PhongCach { get; set; } // songao, thugian, phieuluu, amthuc, giadinh, all
    public string? KhuVuc { get; set; } // north, central, south, all
    public int SoDem { get; set; } = 2;
    public int SoKhach { get; set; } = 2;
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
}

public class TrichXuatYChinhResult
{
    public string ChuDe { get; set; } = "goi_y_du_lich"; // goi_y_du_lich, gia_phong, voucher
    public decimal? NganSach { get; set; }
    public int? ThangDi { get; set; }
    public string? PhongCach { get; set; }
    public string? KhuVuc { get; set; }
    public int SoDem { get; set; } = 2;
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

public class GoiYDuLichService(
    DACNContext db,
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<GoiYDuLichService> logger)
{
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
            return Math.Clamp(diemGoc > 1m ? diemGoc / 1000m : diemGoc, 0.75m, 1.0m);

        int Dist(int a, int b)
        {
            var d = Math.Abs(a - b);
            return Math.Min(d, 12 - d);
        }

        var minDist = Math.Min(Dist(thang, batDau), Dist(thang, ketThuc));
        return Math.Max(0.35m, 0.85m - minDist * 0.12m);
    }

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
        else if (q.Contains("mùa đông") || q.Contains("cuối năm") || q.Contains("noel"))
            res.ThangDi = 12;
        else if (q.Contains("tết") || q.Contains("mùa xuân") || q.Contains("đầu năm"))
            res.ThangDi = 2;

        // 3. Trích xuất Gu trải nghiệm
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

        // 4. Trích xuất khu vực
        if (q.Contains("đà nẵng") || q.Contains("hội an") || q.Contains("huế") || q.Contains("miền trung"))
            res.KhuVuc = "central";
        else if (q.Contains("sa pa") || q.Contains("sapa") || q.Contains("hà nội") || q.Contains("hạ long") || q.Contains("miền bắc"))
            res.KhuVuc = "north";
        else if (q.Contains("phú quốc") || q.Contains("đà lạt") || q.Contains("miền nam") || q.Contains("vũng tàu"))
            res.KhuVuc = "south";

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

        // Kết hợp từ bảng GoiYTour_AI + MuaDuLich + GoiYDiaDiemTheoMua_AI
        foreach (var tour in tours)
        {
            var matchedSeason = seasons.FirstOrDefault(s =>
                string.Equals(s.LoaiMua, tour.MuaDuLich, StringComparison.OrdinalIgnoreCase))
                ?? seasons.FirstOrDefault();

            var batDau = matchedSeason?.ThangBatDau ?? 3;
            var ketThuc = matchedSeason?.ThangKetThuc ?? 8;
            var khuVuc = matchedSeason?.KhuVuc ?? XacDinhKhuVucTuDiemDen(tour.DiemDen);

            if (!string.IsNullOrWhiteSpace(req.KhuVuc) && !req.KhuVuc.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(khuVuc, req.KhuVuc, StringComparison.OrdinalIgnoreCase) &&
                    !tour.DiemDen.Contains(req.KhuVuc, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var guList = PhanTichGu(tour.PhongCach, tour.MoTa, tour.TieuDe);
            var sMua = TinhDiemMua(thang, batDau, ketThuc, 0.95m);
            var sBudget = TinhDiemNganSach(req.NganSach, tour.GiaToiThieu, tour.GiaToiDa);
            var sGu = TinhDiemGu(req.PhongCach, guList);
            var sRating = Math.Clamp((tour.DiemDanhGiaTrungBinh ?? 4.7m) / 5.0m, 0.70m, 1.0m);

            var totalScore = (0.40m * sMua + 0.30m * sBudget + 0.20m * sGu + 0.10m * sRating) * 100m;

            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach);
            var relatedSpot = seasonalAttractions
                .FirstOrDefault(sa => sa.MaMua == matchedSeason?.MaId);

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = tour.MaId,
                LoaiGoiY = "Lịch trình AI đề xuất",
                TieuDe = tour.TieuDe,
                DiemDen = tour.DiemDen,
                KhuVuc = khuVuc,
                TenMuaDepNhat = matchedSeason?.Ten ?? $"Mùa {tour.MuaDuLich}",
                ThoiDiemDepNhat = $"Tháng {batDau} – Tháng {ketThuc}",
                ThoiTietDuKien = relatedSpot?.LoaiThoiTiet ?? matchedSeason?.MoTaThoiTiet ?? "Thời tiết đẹp, thuận lợi check-in & nghỉ dưỡng",
                ChiPhiDuKienTu = tour.GiaToiThieu,
                ChiPhiDuKienDen = tour.GiaToiDa,
                SoNgayGoiY = tour.SoNgay,
                DanhSachGu = guList,
                LyDoAiGoiY = relatedSpot?.LyDoGoiY ?? tour.MoTa ?? "Điểm đến lý tưởng theo ngân sách và thời điểm bạn chọn.",
                DiemCheckInNoiBat = relatedSpot?.MaDiaDiemNavigation?.Ten ?? tour.DiemDen,
                DuongDanAnh = tour.DuongDanAnh,
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(totalScore, 65m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? "Deluxe Ocean View",
                GiaPhongMoiDem = bestRoom?.GiaCoBan ?? 850_000m
            });
        }

        // Bổ sung các điểm đến từ GoiYDiaDiemTheoMua_AI nếu chưa đủ phong phú
        foreach (var sa in seasonalAttractions)
        {
            if (sa.MaDiaDiemNavigation == null || !sa.MaDiaDiemNavigation.DangHoatDong)
                continue;

            var khuVuc = sa.MaMuaNavigation?.KhuVuc ?? "central";
            if (!string.IsNullOrWhiteSpace(req.KhuVuc) && !req.KhuVuc.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(khuVuc, req.KhuVuc, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var guList = PhanTichGu(sa.MaDiaDiemNavigation.PhongCach, sa.LyDoGoiY, sa.MaDiaDiemNavigation.Ten);
            var bestRoom = ChonPhongPhuHop(rooms, req.NganSach, soDem, req.SoKhach);
            var roomCost = (bestRoom?.GiaCoBan ?? 650_000m) * soDem;
            var estMin = roomCost + 500_000m;
            var estMax = roomCost + 1_500_000m;

            var sMua = TinhDiemMua(thang, sa.ThangBatDauGoiY, sa.ThangKetThucGoiY, sa.DiemPhuHop);
            var sBudget = TinhDiemNganSach(req.NganSach, estMin, estMax);
            var sGu = TinhDiemGu(req.PhongCach, guList);

            var totalScore = (0.40m * sMua + 0.30m * sBudget + 0.20m * sGu + 0.10m * 0.95m) * 100m;

            results.Add(new GoiYKetQuaDto
            {
                MaTourHoacDiaDiem = 1000 + sa.MaId,
                LoaiGoiY = "Điểm Check-in Theo Mùa",
                TieuDe = $"Khám phá {sa.MaDiaDiemNavigation.Ten}",
                DiemDen = sa.MaDiaDiemNavigation.DiaChi ?? "Đà Nẵng - Hội An",
                KhuVuc = khuVuc,
                TenMuaDepNhat = sa.MaMuaNavigation?.Ten ?? "Mùa đẹp nhất trong năm",
                ThoiDiemDepNhat = $"Tháng {sa.ThangBatDauGoiY} – Tháng {sa.ThangKetThucGoiY}",
                ThoiTietDuKien = sa.LoaiThoiTiet ?? "Nắng đẹp, mát mẻ",
                ChiPhiDuKienTu = estMin,
                ChiPhiDuKienDen = estMax,
                SoNgayGoiY = soDem + 1,
                DanhSachGu = guList,
                LyDoAiGoiY = sa.LyDoGoiY ?? sa.MaDiaDiemNavigation.MoTa ?? "Điểm check-in nổi bật được AI đánh giá độ phù hợp cao.",
                DiemCheckInNoiBat = sa.MaDiaDiemNavigation.Ten,
                DuongDanAnh = sa.MaDiaDiemNavigation.DuongDanAnh,
                PhanTramPhuHop = (int)Math.Round(Math.Clamp(totalScore, 65m, 99m)),
                HangPhongPhuHop = bestRoom?.Ten ?? "Standard Double",
                GiaPhongMoiDem = bestRoom?.GiaCoBan ?? 550_000m
            });
        }

        return results
            .OrderByDescending(r => r.PhanTramPhuHop)
            .ThenBy(r => r.ChiPhiDuKienTu)
            .Take(8)
            .ToList();
    }

    public async Task<ChatAiKetQuaDto> ChatAsync(string cauHoi, string? maPhienChat, int? maNguoiDung)
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
        var goiYs = await GoiYDuLichAsync(new GoiYDuLichRequest
        {
            NganSach = intent.NganSach,
            ThangDi = intent.ThangDi,
            PhongCach = intent.PhongCach,
            KhuVuc = intent.KhuVuc,
            SoDem = intent.SoDem
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
            var monthInfo = intent.ThangDi.HasValue ? $"vào tháng {intent.ThangDi.Value}" : $"trong thời điểm hiện tại (tháng {now.Month})";
            var reply = $"Gợi ý hàng đầu {budgetInfo} {monthInfo} là **{best.TieuDe}** tại **{best.DiemDen}** (Độ phù hợp AI: **{best.PhanTramPhuHop}%**). " +
                        $"Thời điểm đẹp nhất: {best.ThoiDiemDepNhat} ({best.ThoiTietDuKien}). " +
                        $"{best.LyDoAiGoiY} " +
                        $"Hạng phòng phù hợp để lưu trú: **{best.HangPhongPhuHop}** ({best.GiaPhongMoiDem:N0}đ/đêm, dự kiến trọn gói từ {best.ChiPhiDuKienTu:N0}đ – {best.ChiPhiDuKienDen:N0}đ).";

            var ctx = string.Join("\n", topGoiY.Select(g =>
                $"- {g.TieuDe} ({g.DiemDen}): Match {g.PhanTramPhuHop}%, mùa đẹp {g.ThoiDiemDepNhat}, thời tiết {g.ThoiTietDuKien}, chi phí {g.ChiPhiDuKienTu:N0}-{g.ChiPhiDuKienDen:N0}đ, phòng gợi ý {g.HangPhongPhuHop} ({g.GiaPhongMoiDem:N0}đ/đêm). Lý do: {g.LyDoAiGoiY}"));

            return (reply, ctx);
        }

        return ("Trợ lý AI BookingStay gợi ý bạn khám phá Biển Mỹ Khê, Bán đảo Sơn Trà hoặc Phố cổ Hội An (thời điểm đẹp nhất từ tháng 3 đến tháng 8), kết hợp nghỉ dưỡng tại hạng phòng Deluxe hướng biển.", "Điểm đến: Đà Nẵng - Hội An - Sơn Trà");
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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var prompt = $"""
                Bạn là Trợ lý AI Tư vấn Du lịch & Đặt phòng của BookingStay.AI.
                Trả lời ngắn gọn (3-4 câu), thân thiện bằng tiếng Việt, CHỈ dựa trên dữ liệu thực tế từ CSDL dưới đây (không tự bịa giá phòng hay mã voucher ngoài dữ liệu):
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

            var resp = await client.PostAsJsonAsync(url, payload, cts.Token);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
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

    private static LoaiPhong? ChonPhongPhuHop(List<LoaiPhong> rooms, decimal? nganSach, int soDem, int soKhach)
    {
        if (rooms.Count == 0) return null;
        var suitableByGuest = rooms.Where(r => r.SoNguoiLonToiDa >= soKhach).ToList();
        var pool = suitableByGuest.Count > 0 ? suitableByGuest : rooms;

        if (!nganSach.HasValue || nganSach.Value <= 0)
            return pool.First();

        // Dành tối đa ~65% tổng ngân sách cho lưu trú
        var maxRoomBudgetPerNight = nganSach.Value * 0.65m / Math.Max(1, soDem);
        var affordable = pool.Where(r => r.GiaCoBan <= maxRoomBudgetPerNight).OrderByDescending(r => r.GiaCoBan).FirstOrDefault();
        return affordable ?? pool.OrderBy(r => r.GiaCoBan).First();
    }

    private static string XacDinhKhuVucTuDiemDen(string diemDen)
    {
        var d = diemDen.ToLowerInvariant();
        if (d.Contains("sa pa") || d.Contains("hà nội") || d.Contains("hạ long") || d.Contains("ninh bình"))
            return "north";
        if (d.Contains("phú quốc") || d.Contains("đà lạt") || d.Contains("côn đảo") || d.Contains("hồ chí minh"))
            return "south";
        return "central";
    }
}
