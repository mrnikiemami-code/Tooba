using System.Globalization;

namespace Tooba.BuildingBlocks.Localization;

/// <summary>
/// مشارکت‌کنندهٔ ماژولی برای عنوان خطای محلی‌سازی‌شده — بدون switch مرکزی ماژول‌محور.
/// </summary>
public interface IErrorMessageContributor
{
    /// <summary>اگر کلید متعلق به این مشارکت‌کننده باشد عنوان را برمی‌گرداند.</summary>
    bool TryLocalize(
        string localizationKey,
        CultureInfo culture,
        IReadOnlyDictionary<string, string?> arguments,
        out string title);
}

/// <summary>قرارداد محلی‌سازی عنوان خطا از localization key.</summary>
public interface IErrorMessageLocalizer
{
    /// <summary>عنوان امن محلی‌سازی‌شده یا fallback.</summary>
    string Localize(
        string localizationKey,
        CultureInfo culture,
        IReadOnlyDictionary<string, string?> arguments,
        string safeFallback);
}

/// <summary>ترکیب مشارکت‌کنندگان ماژولی + fallback امن.</summary>
public sealed class CompositeErrorMessageLocalizer : IErrorMessageLocalizer
{
    private readonly IEnumerable<IErrorMessageContributor> _contributors;

    /// <summary>Localizer را با مشارکت‌کنندگان ثبت‌شده می‌سازد.</summary>
    public CompositeErrorMessageLocalizer(IEnumerable<IErrorMessageContributor> contributors)
        => _contributors = contributors;

    /// <inheritdoc />
    public string Localize(
        string localizationKey,
        CultureInfo culture,
        IReadOnlyDictionary<string, string?> arguments,
        string safeFallback)
    {
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
}
