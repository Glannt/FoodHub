using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FoodHub.Application.Common;

/// <summary>
/// Tiện ích chuẩn hoá chuỗi thành slug URL thân thiện (hỗ trợ tiếng Việt có dấu).
/// </summary>
public static partial class SlugHelper
{
    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex InvalidCharactersRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphensRegex();

    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Bước 1: Thay thế các ký tự đặc biệt của tiếng Việt trước khi normalize
        var normalized = text.Trim().ToLowerInvariant()
            .Replace("đ", "d")
            .Replace("Đ", "d");

        // Bước 2: Tách dấu Unicode (FormD) rồi loại bỏ dấu thanh/nón
        var decomposed = normalized.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var cleanText = sb.ToString().Normalize(NormalizationForm.FormC);

        // Bước 3: Thay thế ký tự không phải chữ/số/khoảng trắng/dấu gạch ngang
        cleanText = InvalidCharactersRegex().Replace(cleanText, "");

        // Bước 4: Chuyển khoảng trắng thành dấu gạch ngang
        cleanText = MultipleSpacesRegex().Replace(cleanText, "-");

        // Bước 5: Gộp nhiều dấu gạch ngang liền nhau thành 1
        cleanText = MultipleHyphensRegex().Replace(cleanText, "-");

        // Bước 6: Cắt gọt dấu gạch ngang ở 2 đầu
        return cleanText.Trim('-');
    }
}

