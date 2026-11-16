using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DACN.Web.Common;

/// <summary>
/// Tiện ích chuẩn hóa thời gian theo múi giờ Việt Nam (UTC+7) và tạo Slug không dấu.
/// </summary>
public static class GioVN
{
    private static readonly TimeZoneInfo VnTimeZone = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam Time", "ICT");
            }
        }
    }

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTimeZone);

    public static DateTime HomNay => Now.Date;

    public static DateTime DauThang
    {
        get
        {
            var n = Now;
            return new DateTime(n.Year, n.Month, 1);
        }
    }

    /// <summary>
    /// Chuyển tiêu đề tiếng Việt có dấu sang chuỗi URL slug chuẩn ASCII không dấu.
    /// Ví dụ: "Khám Phá Bán Đảo Sơn Trà!" -> "kham-pha-ban-dao-son-tra"
    /// </summary>
    public static string TaoSlug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "bai-viet";

        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            sb.Append(c switch
            {
                'đ' => 'd',
                'Đ' => 'd',
                _ => c
            });
        }

        var ascii = sb.ToString().Normalize(NormalizationForm.FormC);
        var slug = Regex.Replace(ascii, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');

        return string.IsNullOrEmpty(slug) ? "bai-viet" : slug;
    }
}
