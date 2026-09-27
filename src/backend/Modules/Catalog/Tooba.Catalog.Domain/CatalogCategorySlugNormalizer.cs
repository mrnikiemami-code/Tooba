using System.Globalization;
using System.Text;

namespace Tooba.Catalog.Domain;

/// <summary>
/// نرمال‌سازی locale و slug محلی رده (kebab، lowercase، trim).
/// </summary>
public static class CatalogCategorySlugNormalizer
{
    /// <summary>locale را trim می‌کند؛ خالی ممنوع است.</summary>
    public static string NormalizeLocale(string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        return locale.Trim();
    }

    /// <summary>
    /// slug را به شکل lowercase + kebab نرمال می‌کند؛ حروف یونیکد (مثلاً فارسی) حفظ می‌شوند.
    /// </summary>
    public static string NormalizeSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        if (!TryBuildNormalizedSlug(slug, out var normalized))
        {
            throw new InvalidOperationException("slug رده پس از نرمال‌سازی خالی شد.");
        }

        return normalized;
    }

    /// <summary>
    /// همان هستهٔ نرمال‌سازی <see cref="NormalizeSlug"/> بدون پرتاب برای ورودی نامعتبر.
    /// </summary>
    public static bool TryNormalizeSlug(string? slug, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            normalized = string.Empty;
            return false;
        }

        return TryBuildNormalizedSlug(slug, out normalized);
    }

    /// <summary>از نام نمایشی یک slug اولیه می‌سازد.</summary>
    public static string SlugifyFromName(string name) => NormalizeSlug(name);

    /// <summary>
    /// همان هستهٔ <see cref="SlugifyFromName"/> بدون پرتاب برای ورودی نامعتبر.
    /// </summary>
    public static bool TrySlugifyFromName(string? name, out string normalized)
        => TryNormalizeSlug(name, out normalized);

    /// <summary>
    /// هستهٔ مشترک نرمال‌سازی kebab؛ خروجی خالی = ناموفق (بدون پرتاب).
    /// </summary>
    private static bool TryBuildNormalizedSlug(string slug, out string normalized)
    {
        var trimmed = slug.Trim().ToLower(CultureInfo.InvariantCulture);
        var builder = new StringBuilder(trimmed.Length);
        var pendingHyphen = false;
        foreach (var ch in trimmed)
        {
            if (char.IsWhiteSpace(ch) || ch is '_' or '/' or '\\')
            {
                pendingHyphen = builder.Length > 0;
                continue;
            }

            if (ch == '-')
            {
                pendingHyphen = builder.Length > 0;
                continue;
            }

            if (char.IsLetterOrDigit(ch) || ch > 127)
            {
                if (pendingHyphen)
                {
                    builder.Append('-');
                    pendingHyphen = false;
                }

                builder.Append(ch);
            }
        }

        var result = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(result))
        {
            normalized = string.Empty;
            return false;
        }

        normalized = result;
        return true;
    }
}
