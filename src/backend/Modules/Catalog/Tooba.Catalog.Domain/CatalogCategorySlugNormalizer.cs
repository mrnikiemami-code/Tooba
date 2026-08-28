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
            throw new InvalidOperationException("slug رده پس از نرمال‌سازی خالی شد.");
        }

        return result;
    }

    /// <summary>از نام نمایشی یک slug اولیه می‌سازد.</summary>
    public static string SlugifyFromName(string name) => NormalizeSlug(name);
}
