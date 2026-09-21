using System.Globalization;

namespace Tooba.BuildingBlocks.Localization;

/// <summary>گزینه‌های resolver زبان درخواست.</summary>
public sealed class RequestLocaleOptions
{
    /// <summary>نام بخش پیکربندی.</summary>
    public const string SectionName = "Tooba:Localization:RequestLocale";

    /// <summary>فرهنگ پیش‌فرض وقتی هیچ کاندیدی معتبر نباشد (نه FA-first).</summary>
    public string DefaultCulture { get; set; } = "en";

    /// <summary>زنجیرهٔ fallback پیکربندی‌شده پس از تطبیق ناقص.</summary>
    public List<string> FallbackCultures { get; set; } = ["en"];

    /// <summary>فرهنگ‌های پشتیبانی‌شده؛ خالی = همهٔ فرهنگ‌های شناخته‌شدهٔ .NET.</summary>
    public List<string> SupportedCultures { get; set; } = [];
}

/// <summary>قرارداد resolve زبان از Accept-Language یا مقدار خام.</summary>
public interface IRequestLocaleResolver
{
    /// <summary>فرهنگ canonical را از هدر Accept-Language resolve می‌کند.</summary>
    CultureInfo Resolve(string? acceptLanguageHeader);

    /// <summary>نرمال‌سازی نام فرهنگ (مثلاً en-US → en-US).</summary>
    string Normalize(string? cultureName);
}

/// <summary>
/// Resolver مرکزی Accept-Language — بدون Contains("en") ad-hoc و بدون FA-first.
/// </summary>
public sealed class RequestLocaleResolver : IRequestLocaleResolver
{
    private readonly RequestLocaleOptions _options;

    /// <summary>Resolver را با گزینه‌ها می‌سازد.</summary>
    public RequestLocaleResolver(Microsoft.Extensions.Options.IOptions<RequestLocaleOptions> options)
        => _options = options.Value;

    /// <summary>Resolver تست با گزینه‌های صریح.</summary>
    public RequestLocaleResolver(RequestLocaleOptions options) => _options = options;

    /// <inheritdoc />
    public CultureInfo Resolve(string? acceptLanguageHeader)
    {
        foreach (var candidate in ParseAcceptLanguage(acceptLanguageHeader))
        {
            if (TryCreateCulture(candidate, out var culture) && IsSupported(culture))
            {
                return culture;
            }

            var languageOnly = GetLanguageOnly(candidate);
            if (languageOnly is not null
                && TryCreateCulture(languageOnly, out var languageCulture)
                && IsSupported(languageCulture))
            {
                return languageCulture;
            }
        }

        foreach (var fallback in _options.FallbackCultures)
        {
            if (TryCreateCulture(fallback, out var culture) && IsSupported(culture))
            {
                return culture;
            }
        }

        return TryCreateCulture(_options.DefaultCulture, out var defaults)
            ? defaults
            : CultureInfo.GetCultureInfo("en");
    }

    /// <inheritdoc />
    public string Normalize(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return _options.DefaultCulture;
        }

        return TryCreateCulture(cultureName.Trim(), out var culture)
            ? culture.Name
            : _options.DefaultCulture;
    }

    private bool IsSupported(CultureInfo culture)
    {
        if (_options.SupportedCultures.Count == 0)
        {
            return true;
        }

        return _options.SupportedCultures.Any(s =>
            string.Equals(s, culture.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ParseAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            yield break;
        }

        var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var scored = new List<(string Tag, double Q)>();
        foreach (var part in parts)
        {
            var segments = part.Split(';', StringSplitOptions.TrimEntries);
            var tag = segments[0].Trim();
            if (string.IsNullOrWhiteSpace(tag) || tag == "*")
            {
                continue;
            }

            var q = 1.0;
            for (var i = 1; i < segments.Length; i++)
            {
                var param = segments[i];
                if (param.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(param[2..], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                {
                    q = parsed;
                }
            }

            if (q <= 0)
            {
                continue;
            }

            scored.Add((tag, q));
        }

        foreach (var item in scored.OrderByDescending(x => x.Q))
        {
            yield return item.Tag;
        }
    }

    private static string? GetLanguageOnly(string tag)
    {
        var dash = tag.IndexOf('-');
        return dash > 0 ? tag[..dash] : null;
    }

    private static bool TryCreateCulture(string name, out CultureInfo culture)
    {
        culture = CultureInfo.InvariantCulture;
        try
        {
            culture = CultureInfo.GetCultureInfo(name);
            return !Equals(culture, CultureInfo.InvariantCulture) || name.Equals("iv", StringComparison.OrdinalIgnoreCase);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
