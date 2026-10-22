using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Application.Authoring;

/// <summary>
/// projection خالص و مشترک زمینهٔ نویسندگی: انتخاب زبان‌های <b>فعال</b>، اعتبارسنجی readiness و ساخت
/// نمای typed. این helper همان الگوریتم TB-188 است و فقط از یک جا استفاده می‌شود تا capability
/// ارائهٔ چندزبانه نسخهٔ دوم آن را تکرار نکند.
/// </summary>
/// <remarks>
/// هیچ زبان/ارز/حالتی اینجا فعال یا اصلاح نمی‌شود و هیچ ترتیب نمایشی از locale ساخته نمی‌شود. تنها
/// تفاوت با نسخهٔ درون‌هندلری، بسته‌بندی نتیجه است؛ رفتار/کد خطا/ترتیب/فیلترها تغییر نمی‌کند.
/// </remarks>
internal static class ShippingAuthoringContextProjection
{
    /// <summary>زمینهٔ typed یا شکست canonical <c>configuration_unavailable</c>.</summary>
    /// <param name="snapshot">snapshot منابع authoritative.</param>
    public static Result<ShippingAuthoringContextView> Project(ShippingAuthoringContextSourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var edition = snapshot.Edition switch
        {
            ToobaEdition.SingleStore => "SingleStore",
            ToobaEdition.Marketplace => "Marketplace",
            _ => null,
        };
        if (edition is null)
        {
            return Unavailable();
        }

        var currency = snapshot.DefaultCurrency?.Trim();
        if (string.IsNullOrEmpty(currency))
        {
            return Unavailable();
        }

        // فقط ردیف‌هایی که مالک واقعاً فعال کرده است؛ نبود flag هرگز «فعال» خوانده نمی‌شود.
        var active = snapshot.Languages
            .Where(language => language.IsActive == true)
            .ToList();
        if (active.Count == 0)
        {
            return Unavailable();
        }

        if (active.Any(language => !IsUsable(language)))
        {
            return Unavailable();
        }

        var defaults = active.Where(language => language.IsDefault).ToList();
        if (defaults.Count != 1)
        {
            // صفر یعنی پیش‌فرض فقط روی ردیف غیرفعال اعلام شده است؛ بیش از یکی یعنی ابهام.
            return Unavailable();
        }

        var defaultLanguage = defaults[0];

        // ترتیب مالک (SortOrder) حفظ می‌شود؛ OrderBy پایدار است.
        var languages = active
            .OrderBy(language => language.SortOrder!.Value)
            .Select(language => new ShippingAuthoringLanguageView(
                language.LanguageId,
                language.Code.Trim(),
                language.Culture.Trim(),
                (language.UrlPrefix ?? string.Empty).Trim(),
                language.Direction!.Trim().ToLowerInvariant(),
                language.DisplayName!.Trim(),
                language.NativeName!.Trim(),
                language.IsDefault,
                language.SortOrder!.Value))
            .ToList();

        return Result.Success(new ShippingAuthoringContextView(
            edition,
            currency,
            languages,
            defaultLanguage.LanguageId));
    }

    /// <summary>
    /// metadata لازم برای یک زبان محتوای معتبر: هویت، جهت واقعی و نام‌های واقعی و ترتیب مالک.
    /// پیشوند مسیر عمداً الزامی نیست، چون زبان پیش‌فرض می‌تواند بدون پیشوند باشد.
    /// </summary>
    private static bool IsUsable(ShippingAuthoringLanguageSource language) =>
        language.LanguageId != Guid.Empty
        && !string.IsNullOrWhiteSpace(language.Code)
        && !string.IsNullOrWhiteSpace(language.Culture)
        && !string.IsNullOrWhiteSpace(language.DisplayName)
        && !string.IsNullOrWhiteSpace(language.NativeName)
        && IsKnownDirection(language.Direction)
        && language.SortOrder is not null;

    private static bool IsKnownDirection(string? direction) =>
        string.Equals(direction?.Trim(), "rtl", StringComparison.OrdinalIgnoreCase)
        || string.Equals(direction?.Trim(), "ltr", StringComparison.OrdinalIgnoreCase);

    private static Result<ShippingAuthoringContextView> Unavailable() =>
        Result.Failure<ShippingAuthoringContextView>(
            new SemanticError(ShippingErrorCodes.AuthoringConfigurationUnavailable));
}
