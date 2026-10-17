using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.OwnedRate;

/// <summary>نامزد نرخ‌گیری: هویت متعارف همان روشی که مسیر قدیمی واجد شرایط دانسته است.</summary>
/// <param name="MethodCode">کد روش متعارف (مثلاً <c>tipax:standard</c>).</param>
/// <param name="ServiceCode">کد سرویس مالک.</param>
/// <param name="OptionCode">کد گزینهٔ مالک؛ برای خدمت بدون گزینه تهی است.</param>
public sealed record StorefrontShippingOwnedRateCandidate(
    string MethodCode,
    string ServiceCode,
    string? OptionCode);

/// <summary>نرخ authoritative پذیرفته‌شده برای یک روش.</summary>
/// <param name="MethodCode">کد روش متعارف.</param>
/// <param name="Amount">مبلغ پذیرفته‌شده از Shipping.</param>
/// <param name="CurrencyCode">ارز همان نرخ.</param>
/// <param name="LeadDays">
/// روزهای حمل اعلام‌شدهٔ همان نرخ مالک؛ هیچ وعدهٔ تحویل از جای دیگری ساخته نمی‌شود.
/// </param>
public sealed record StorefrontShippingOwnedRateQuote(
    string MethodCode,
    decimal Amount,
    string? CurrencyCode,
    int LeadDays = 0);

/// <summary>دلیل پایدار و قابل‌اعلامِ نبودِ نرخ authoritative برای یک روش.</summary>
/// <param name="MethodCode">کد روش متعارف.</param>
/// <param name="ReasonCode">کد دلیل پایدار؛ هرگز مبلغ یا مقدار ساختگی جای آن نیست.</param>
public sealed record StorefrontShippingOwnedRateUnavailable(string MethodCode, string ReasonCode);

/// <summary>
/// نتیجهٔ یک دور نرخ‌گیری مالک‌شده.
/// </summary>
/// <remarks>
/// <see cref="Evaluated"/> = <c>false</c> صریحاً یعنی دروازه خاموش است و مسیر قدیمی باید
/// <b>دست‌نخورده</b> بماند. وقتی <c>true</c> است، تنها روش‌های <see cref="Quoted"/> قیمت دارند و
/// هر روش دیگر در <see cref="Unavailable"/> با دلیل پایدار آمده است؛ روش بدون نرخ نباید قابل انتخاب باشد.
/// </remarks>
/// <param name="Evaluated">آیا دروازه واقعاً اجرا شد.</param>
/// <param name="Quoted">نرخ‌های پذیرفته‌شده به‌کلید کد روش.</param>
/// <param name="Unavailable">روش‌های ردشده با دلیل پایدار.</param>
public sealed record StorefrontShippingOwnedRateResolution(
    bool Evaluated,
    IReadOnlyDictionary<string, StorefrontShippingOwnedRateQuote> Quoted,
    IReadOnlyList<StorefrontShippingOwnedRateUnavailable> Unavailable)
{
    /// <summary>دروازهٔ خاموش: هیچ نرخی جابه‌جا نمی‌شود.</summary>
    public static StorefrontShippingOwnedRateResolution Disabled { get; } = new(
        false,
        new Dictionary<string, StorefrontShippingOwnedRateQuote>(StringComparer.OrdinalIgnoreCase),
        []);
}

/// <summary>کدهای دلیل پایدارِ لایهٔ Order برای رد نرخ.</summary>
public static class StorefrontShippingOwnedRateReasons
{
    /// <summary>درز Shipping در دسترس نیست (ثبت‌نشده/خطای غیرمنتظره).</summary>
    public const string OwnerUnavailable = "shipping.owned_rate.owner_unavailable";

    /// <summary>هویت گزینهٔ روش ناقص است؛ تطبیق سخت‌گیرانه معنا ندارد.</summary>
    public const string MethodIdentityIncomplete = "shipping.owned_rate.method_identity_incomplete";

    /// <summary>مقصد با حل قطعی Geography تأیید نشد؛ هیچ حدسی ساخته نمی‌شود.</summary>
    public const string DestinationUnresolved = "shipping.owned_rate.destination_unresolved";

    /// <summary>نرخ مالک‌شده‌ای وجود ندارد (شامل ردیف سطح‌خدمت/گزینهٔ هم‌خانواده که مجاز نیست).</summary>
    public const string NotOwned = "shipping.owned_rate.not_owned";

    /// <summary>ارز نرخ با ارز سبد نمی‌خواند.</summary>
    public const string CurrencyMismatch = "shipping.owned_rate.currency_mismatch";

    /// <summary>پرس‌وجوی نرخ شکست خورد (خطای واقعی مرجع).</summary>
    public const string QuoteFailed = "shipping.owned_rate.quote_failed";
}

/// <summary>
/// تنها نقطهٔ پرس‌وجوی نرخ مشتری از مرجع تجاری Shipping در مسیر فروشگاه.
/// </summary>
/// <remarks>
/// <para>
/// <b>خاموش = صفر فراخوانی.</b> تا وقتی <see cref="StorefrontShippingOwnedRateOptions.Enabled"/>
/// برقرار نباشد این نوع هیچ کوئری، هیچ حل مقصد و هیچ لاگی تولید نمی‌کند و مسیر قدیمی عیناً اجرا می‌شود.
/// </para>
/// <para>
/// <b>fail-closed.</b> هر کمبودی (نبود درز، مقصد حل‌نشده، نبود ردیف مالک‌شده، پوشش Restricted، شرط
/// اندازه‌گیری‌شده بدون اندازه، ناهم‌خوانی ارز) به «این روش نرخ معتبر ندارد» ترجمه می‌شود؛ هرگز به صفر،
/// رایگان، نرخ عمومی یا نرخ حامل/provider تبدیل نمی‌شود.
/// </para>
/// <para>
/// <b>اندازهٔ فیزیکی حدس زده نمی‌شود.</b> مسیر فروشگاه هیچ منبع اندازهٔ مورداعتمادی ندارد، پس
/// <see cref="ShippingMeasuredParcel"/> همیشه تهی فرستاده می‌شود و Shipping خودش شرط‌دار را رد می‌کند.
/// </para>
/// </remarks>
public sealed class StorefrontShippingOwnedRateResolver(
    IShippingRateQuoteLookup? lookup,
    IGeographyLookup? geography,
    Microsoft.Extensions.Options.IOptions<StorefrontShippingOwnedRateOptions>? options)
{
    private const string LegacyCountryCode = "IR";

    /// <summary>آیا دروازه روشن است (بدون هیچ فراخوانی).</summary>
    public bool IsEnabled => options?.Value.Enabled == true;

    /// <summary>
    /// برای فهرست روش‌های واجد شرایطِ مسیر قدیمی، نرخ authoritative مالک را می‌گیرد.
    /// </summary>
    /// <param name="candidates">نامزدهای مسیر قدیمی (هویت متعارف روش).</param>
    /// <param name="provinceName">استان انتخاب‌شدهٔ مشتری.</param>
    /// <param name="cityName">شهر انتخاب‌شده در صورت وجود.</param>
    /// <param name="destination">شناسه‌های پایدار Geography در صورت وجود.</param>
    /// <param name="cartSubtotalExclusive">جمع سبد بدون مالیات (ورودی آستانهٔ ارسال رایگان مالک).</param>
    /// <param name="currencyCode">ارز سبد؛ نرخ با ارز دیگر پذیرفته نمی‌شود.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<StorefrontShippingOwnedRateResolution> ResolveAsync(
        IReadOnlyList<StorefrontShippingOwnedRateCandidate> candidates,
        string? provinceName,
        string? cityName,
        DestinationIdentifiers? destination,
        decimal cartSubtotalExclusive,
        string? currencyCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (!IsEnabled)
        {
            return StorefrontShippingOwnedRateResolution.Disabled;
        }

        if (candidates.Count == 0)
        {
            return new StorefrontShippingOwnedRateResolution(
                true,
                new Dictionary<string, StorefrontShippingOwnedRateQuote>(StringComparer.OrdinalIgnoreCase),
                []);
        }

        if (lookup is null)
        {
            return AllUnavailable(candidates, StorefrontShippingOwnedRateReasons.OwnerUnavailable);
        }

        DestinationIdentifiers? resolved;
        try
        {
            resolved = await ResolveDestinationAsync(provinceName, cityName, destination, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return AllUnavailable(candidates, StorefrontShippingOwnedRateReasons.DestinationUnresolved);
        }

        if (resolved is null)
        {
            return AllUnavailable(candidates, StorefrontShippingOwnedRateReasons.DestinationUnresolved);
        }

        var quoted = new Dictionary<string, StorefrontShippingOwnedRateQuote>(StringComparer.OrdinalIgnoreCase);
        var unavailable = new List<StorefrontShippingOwnedRateUnavailable>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var methodCode = (candidate.MethodCode ?? string.Empty).Trim();
            if (methodCode.Length == 0)
            {
                continue;
            }

            if (quoted.ContainsKey(methodCode))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(candidate.ServiceCode))
            {
                unavailable.Add(new StorefrontShippingOwnedRateUnavailable(
                    methodCode, StorefrontShippingOwnedRateReasons.MethodIdentityIncomplete));
                continue;
            }

            var optionCode = string.IsNullOrWhiteSpace(candidate.OptionCode) ? null : candidate.OptionCode.Trim();

            ShippingQuote? quote;
            try
            {
                quote = await lookup.QuoteAsync(
                    new ShippingQuoteRequest(
                        candidate.ServiceCode.Trim(),
                        resolved,
                        cartSubtotalExclusive,
                        currencyCode,
                        optionCode)
                    {
                        // تنها حالت مجاز: مالکیت سخت‌گیرانهٔ گزینه. ردیف سطح‌خدمت/گزینهٔ دیگر قیمت نمی‌دهد.
                        OptionMatchMode = ShippingOptionMatchMode.StrictOwnedOption,
                        // اندازهٔ فیزیکی هیچ منبع مورداعتمادی ندارد و هرگز از متن مشتری/جمع سبد ساخته نمی‌شود.
                        Parcel = null,
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                unavailable.Add(new StorefrontShippingOwnedRateUnavailable(
                    methodCode, StorefrontShippingOwnedRateReasons.QuoteFailed));
                continue;
            }

            if (quote is null)
            {
                unavailable.Add(new StorefrontShippingOwnedRateUnavailable(
                    methodCode, StorefrontShippingOwnedRateReasons.QuoteFailed));
                continue;
            }

            if (!quote.IsQuoted)
            {
                // نبود/رد نرخ هرگز به صفر یا رایگان تبدیل نمی‌شود؛ کد دلیل خود مالک حفظ می‌شود.
                unavailable.Add(new StorefrontShippingOwnedRateUnavailable(
                    methodCode,
                    string.IsNullOrWhiteSpace(quote.ReasonCode)
                        ? StorefrontShippingOwnedRateReasons.NotOwned
                        : quote.ReasonCode));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(currencyCode)
                && !string.IsNullOrWhiteSpace(quote.CurrencyCode)
                && !string.Equals(currencyCode.Trim(), quote.CurrencyCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                unavailable.Add(new StorefrontShippingOwnedRateUnavailable(
                    methodCode, StorefrontShippingOwnedRateReasons.CurrencyMismatch));
                continue;
            }

            if (quote.ChargeAmount < 0m)
            {
                unavailable.Add(new StorefrontShippingOwnedRateUnavailable(
                    methodCode, StorefrontShippingOwnedRateReasons.QuoteFailed));
                continue;
            }

            quoted[methodCode] = new StorefrontShippingOwnedRateQuote(
                methodCode,
                quote.ChargeAmount,
                quote.CurrencyCode,
                quote.LeadDays);
        }

        return new StorefrontShippingOwnedRateResolution(true, quoted, unavailable);
    }

    private static StorefrontShippingOwnedRateResolution AllUnavailable(
        IReadOnlyList<StorefrontShippingOwnedRateCandidate> candidates,
        string reasonCode)
        => new(
            true,
            new Dictionary<string, StorefrontShippingOwnedRateQuote>(StringComparer.OrdinalIgnoreCase),
            candidates
                .Select(c => (c.MethodCode ?? string.Empty).Trim())
                .Where(code => code.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(code => new StorefrontShippingOwnedRateUnavailable(code, reasonCode))
                .ToArray());

    private async Task<DestinationIdentifiers?> ResolveDestinationAsync(
        string? provinceName,
        string? cityName,
        DestinationIdentifiers? destination,
        CancellationToken cancellationToken)
    {
        if (destination is { } explicitIdentifiers)
        {
            if (explicitIdentifiers.CountryId == Guid.Empty || geography is null)
            {
                return null;
            }

            return await geography.GetByIdsAsync(explicitIdentifiers, cancellationToken);
        }

        if (geography is null
            || (string.IsNullOrWhiteSpace(provinceName) && string.IsNullOrWhiteSpace(cityName)))
        {
            return null;
        }

        var resolution = await geography.ResolveLegacyAsync(
            LegacyCountryCode,
            provinceName,
            cityName,
            serviceAreaName: null,
            cancellationToken);

        return resolution.Outcome == DestinationResolutionOutcome.Resolved
            ? resolution.Identifiers
            : null;
    }
}
