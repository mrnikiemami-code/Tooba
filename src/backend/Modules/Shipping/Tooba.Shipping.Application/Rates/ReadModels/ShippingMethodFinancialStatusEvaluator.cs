using System.Globalization;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Application.Rates.ReadModels;

/// <summary>
/// استنتاج قطعیِ وضعیت مالی یک روش canonical از ردیف‌های واقعی نرخ/پوشش. هیچ ورودی/خروجی و هیچ
/// زمان جاری‌ای نمی‌خواند، بنابراین نتیجه فقط از دادهٔ داده‌شده مشتق و قابل بازتولید است.
/// </summary>
/// <remarks>
/// <para>
/// <b>هویت روش پیش از هر چیز.</b> یک ردیف نرخ فقط وقتی «متعلق به این روش» است که سرویس و گزینهٔ
/// آن دقیقاً با هویت authoritative روش برابر باشد. یک ردیف سطح سرویس (<c>optionCode</c> تهی) برای
/// روش گزینه‌دار مالکیت اثبات نمی‌کند و به‌عنوان <see cref="ShippingMethodFinancialState.LegacySharedUnresolved"/>
/// فقط خوانا می‌ماند؛ نرخ هم‌خانوادهٔ گزینهٔ دیگر هرگز به این روش نسبت داده نمی‌شود.
/// </para>
/// <para>
/// <b>نبود داده هرگز رایگان نیست.</b> حالت‌های برگشتی فقط از دادهٔ موجود ساخته می‌شوند و مبلغ
/// سطح سرویس در حالت مشترک قدیمی بازگردانده نمی‌شود تا ادمین مبلغی را نبیند که مالک آن نیست.
/// </para>
/// <para>
/// <b>ابهام حل نمی‌شود.</b> دو قاعدهٔ فعال با دامنه و شرط یکسان، یا دو ارز متفاوت، به
/// <see cref="ShippingMethodFinancialState.Conflicting"/> می‌رسند؛ انتخاب «ردیف اول» یک تصمیم
/// تجاری پنهان است.
/// </para>
/// </remarks>
public static class ShippingMethodFinancialStatusEvaluator
{
    /// <summary>وضعیت مالی یک روش را از دادهٔ داده‌شده استنتاج می‌کند.</summary>
    /// <param name="serviceCode">کد سرویس authoritative.</param>
    /// <param name="optionCode">گزینهٔ authoritative؛ تهی یعنی روش در سطح سرویس است.</param>
    /// <param name="policies">همهٔ سیاست‌های نرخ حوزهٔ همان سرویس؛ ممکن است خالی باشد.</param>
    /// <param name="coverageGrants">همهٔ مجوزهای پوشش حوزهٔ همان سرویس؛ ممکن است خالی باشد.</param>
    /// <returns>نمای قطعی؛ هرگز null.</returns>
    public static ShippingMethodFinancialStatusView Evaluate(
        string? serviceCode,
        string? optionCode,
        IReadOnlyCollection<ShippingRatePolicyAdminItem>? policies,
        IReadOnlyCollection<ShippingCoverageGrantAdminItem>? coverageGrants)
    {
        var service = Normalize(serviceCode) ?? string.Empty;
        var option = Normalize(optionCode);

        var active = (policies ?? Array.Empty<ShippingRatePolicyAdminItem>())
            .Where(x => x is not null)
            .Where(x => x.IsActive)
            .Where(x => string.Equals(Normalize(x.ServiceCode), service, StringComparison.Ordinal))
            .ToArray();

        var owned = active
            .Where(x => string.Equals(Normalize(x.OptionCode), option, StringComparison.Ordinal))
            .ToArray();
        var serviceLevel = active.Where(x => Normalize(x.OptionCode) is null).ToArray();
        var siblings = active
            .Where(x => Normalize(x.OptionCode) is { } other && !string.Equals(other, option, StringComparison.Ordinal))
            .ToArray();

        var activeGrants = (coverageGrants ?? Array.Empty<ShippingCoverageGrantAdminItem>())
            .Where(x => x is not null)
            .Where(x => x.IsActive)
            .Where(x => string.Equals(Normalize(x.ServiceCode), service, StringComparison.Ordinal))
            .Count();

        // The optionless method's own rules ARE the service-level rows; they are never "shared legacy"
        // for that method, because no other option identity exists to leak from.
        if (owned.Length == 0)
        {
            if (option is not null && serviceLevel.Length > 0)
            {
                return new ShippingMethodFinancialStatusView(
                    service,
                    option,
                    ShippingMethodFinancialState.LegacySharedUnresolved,
                    ShippingMethodFinancialMode.None,
                    null,
                    0,
                    0,
                    false,
                    serviceLevel.Length,
                    siblings.Length,
                    activeGrants,
                    Array.Empty<ShippingMethodFinancialRule>(),
                    ShippingMethodFinancialReasonCodes.LegacyServiceLevelShared);
            }

            return new ShippingMethodFinancialStatusView(
                service,
                option,
                ShippingMethodFinancialState.NeedsConfiguration,
                ShippingMethodFinancialMode.None,
                null,
                0,
                0,
                false,
                serviceLevel.Length,
                siblings.Length,
                activeGrants,
                Array.Empty<ShippingMethodFinancialRule>(),
                ShippingMethodFinancialReasonCodes.NoOwnedRules);
        }

        var rules = owned.Select(ToRule).ToArray();
        var currencies = owned
            .Select(x => x.CurrencyCode?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var state = ShippingMethodFinancialState.FinanciallyConfigured;
        var reason = ShippingMethodFinancialReasonCodes.OwnedRulesPresent;

        if (currencies.Length > 1)
        {
            state = ShippingMethodFinancialState.Conflicting;
            reason = ShippingMethodFinancialReasonCodes.MixedCurrency;
        }
        else if (HasDuplicateScopeAndCondition(owned))
        {
            state = ShippingMethodFinancialState.Conflicting;
            reason = ShippingMethodFinancialReasonCodes.AmbiguousOwnedScope;
        }

        var currency = state == ShippingMethodFinancialState.FinanciallyConfigured
            ? currencies.SingleOrDefault()
            : null;

        return new ShippingMethodFinancialStatusView(
            service,
            option,
            state,
            ShippingMethodFinancialMode.FixedAmount,
            currency,
            owned.Length,
            owned.Count(x => x.Condition is not null),
            owned.Any(x => x.ChargeAmount == 0m),
            serviceLevel.Length,
            siblings.Length,
            activeGrants,
            rules,
            reason);
    }

    private static ShippingMethodFinancialRule ToRule(ShippingRatePolicyAdminItem policy)
        => new(
            policy.RatePolicyId,
            policy.Level,
            policy.ChargeAmount,
            policy.CurrencyCode?.Trim() ?? string.Empty,
            policy.PreparationDays,
            policy.LeadDays,
            policy.FreeAboveThreshold,
            policy.Condition is not null);

    /// <summary>
    /// آیا دو قاعدهٔ فعال، دامنه و شرط یکسان دارند. برخورد دقیقاً همان چیزی است که موتور نرخ در
    /// زمان استعلام برای همان مقصد به‌عنوان ابهام گزارش می‌کند، پس در سطح آمادگی ادمین هم قابل
    /// اثبات است و از پیش حدس زده نمی‌شود.
    /// </summary>
    private static bool HasDuplicateScopeAndCondition(
        IReadOnlyCollection<ShippingRatePolicyAdminItem> owned)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return owned.Any(x => !seen.Add(ScopeAndConditionKey(x)));
    }

    private static string ScopeAndConditionKey(ShippingRatePolicyAdminItem policy)
    {
        var condition = policy.Condition;
        var weight = condition?.WeightGrams;
        var count = condition?.ItemCount;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{policy.CountryId}|{policy.AdministrativeAreaId}|{policy.CityId}|{policy.ServiceAreaId}" +
            $"|w:{weight?.Min}..{weight?.Max}|c:{count?.Min}..{count?.Max}");
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant();
    }
}
