using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن زمینهٔ نویسندگی ادمین ارسال. این query هیچ ورودی فراخوان‌کننده‌ای ندارد: حالت نصب، ارز و
/// زبان‌ها همگی از زمینه/منابع authoritative سرور می‌آیند، پس هیچ override مالك/حالت/زبان/ارز از
/// route، query string یا body پذیرفته نمی‌شود.
/// </summary>
public sealed record GetShippingAuthoringContextQuery()
    : IRequest<Result<ShippingAuthoringContextView>>;

/// <summary>
/// هندلر فقط-خواندنی زمینهٔ نویسندگی. کارش projection آماده‌سازی مصرف‌کننده است: انتخاب زبان‌های
/// فعال، اعتبارسنجی readiness و ساخت نمای typed. این هندلر هیچ زبان/ارز/حالتی را فعال یا اصلاح
/// نمی‌کند و سیاست پیش‌فرض Localization را تکرار نمی‌کند.
/// </summary>
public sealed class GetShippingAuthoringContextHandler(IShippingAuthoringContextSource source)
    : IRequestHandler<GetShippingAuthoringContextQuery, Result<ShippingAuthoringContextView>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringContextView>> Handle(
        GetShippingAuthoringContextQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // استثنای وابستگی/لغو عمداً گرفته نمی‌شود: از مرز canonical موجود عبور می‌کند و هرگز به
        // موفقیت ساختگی یا context خالی تبدیل نمی‌شود.
        var snapshot = await source.ReadAsync(cancellationToken);

        var edition = snapshot.Edition switch
        {
            Tooba.BuildingBlocks.ToobaEdition.SingleStore => "SingleStore",
            Tooba.BuildingBlocks.ToobaEdition.Marketplace => "Marketplace",
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
            // صفر یعنی پیش‌فرض فقط روی ردیف غیرفعال اعلام شده است؛ بیش از یکی یعنی ابهام. در هر دو
            // حالت انتخاب پیش‌فرض یک تصمیم پنهان می‌شد.
            return Unavailable();
        }

        var defaultLanguage = defaults[0];

        // ترتیب مالک (SortOrder) حفظ می‌شود؛ OrderBy پایدار است، پس ردیف‌های هم‌ترتیب ترتیب مرجع را
        // نگه می‌دارند و هیچ ترتیب نمایشی از locale یا سایت‌پذیر جعلی ساخته نمی‌شود.
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
