using System.Globalization;
using System.Resources;

namespace Tooba.BuildingBlocks.Localization;

/// <summary>مجموعهٔ منبع خطا با مالکیت کلید (بدون شاخه‌بندی زبان دستی).</summary>
public interface IErrorResourceSet
{
    /// <summary>آیا این مجموعه مالک کلید است؟</summary>
    bool Owns(string localizationKey);

    /// <summary>رشتهٔ منبع برای فرهنگ؛ null اگر موجود نباشد.</summary>
    string? GetString(string localizationKey, CultureInfo culture);
}

/// <summary>منابع foundation عمومی (validation / platform).</summary>
public static class FoundationErrorResources
{
    /// <summary>ResourceManager برای FoundationErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.BuildingBlocks.Localization.Resources.FoundationErrors", typeof(FoundationErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع foundation.</summary>
public sealed class FoundationErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("validation.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("platform.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        FoundationErrorResources.Manager.GetString(localizationKey, culture);
}

/// <summary>
/// محلی‌سازی عنوان خطا از ResourceManager — culture از resolver مرکزی؛ بدون FA-first.
/// </summary>
public sealed class ResourceErrorMessageLocalizer : IErrorMessageLocalizer
{
    private readonly IEnumerable<IErrorResourceSet> _resourceSets;
    private readonly IEnumerable<IErrorMessageContributor> _contributors;

    /// <summary>Localizer را با مجموعه‌های منبع و مشارکت‌کنندگان اختیاری می‌سازد.</summary>
    public ResourceErrorMessageLocalizer(
        IEnumerable<IErrorResourceSet> resourceSets,
        IEnumerable<IErrorMessageContributor> contributors)
    {
        _resourceSets = resourceSets ?? Array.Empty<IErrorResourceSet>();
        _contributors = contributors ?? Array.Empty<IErrorMessageContributor>();
    }

    /// <inheritdoc />
    public string Localize(
        string localizationKey,
        CultureInfo culture,
        IReadOnlyDictionary<string, string?> arguments,
        string safeFallback)
    {
        ArgumentNullException.ThrowIfNull(culture);

        foreach (var set in _resourceSets)
        {
            if (!set.Owns(localizationKey))
            {
                continue;
            }

            var raw = set.GetString(localizationKey, culture);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                return FormatNamed(raw, arguments);
            }
        }

        foreach (var contributor in _contributors)
        {
            if (contributor.TryLocalize(localizationKey, culture, arguments, out var title)
                && !string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }

        return string.IsNullOrWhiteSpace(safeFallback) ? localizationKey : safeFallback;
    }

    private static string FormatNamed(string template, IReadOnlyDictionary<string, string?> arguments)
    {
        if (arguments.Count == 0)
        {
            return template;
        }

        var result = template;
        foreach (var (key, value) in arguments)
        {
            result = result.Replace("{" + key + "}", string.IsNullOrWhiteSpace(value) ? "?" : value, StringComparison.Ordinal);
        }

        return result;
    }
}
