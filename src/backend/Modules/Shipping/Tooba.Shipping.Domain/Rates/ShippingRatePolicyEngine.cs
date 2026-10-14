using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Domain.Rates;

/// <summary>
/// موتور قطعی و فقط-محاسباتی سیاست نرخ و پوشش ارسال. هیچ ورودی/خروجی، هیچ DbContext و هیچ
/// زمان جاری‌ای نمی‌خواند تا نتیجه فقط از دادهٔ داده‌شده مشتق شود و قابل بازتولید باشد.
/// </summary>
/// <remarks>
/// <para>
/// <b>Fail-closed.</b> نبود مجوز پوشش فعال برای کد خدمت و مقصد، صریحاً
/// <see cref="ShippingServiceabilityOutcome.NotCovered"/> است. هیچ مسیری در این موتور به
/// «سرویس‌پذیر به‌طور پیش‌فرض» نمی‌رسد: حتی فهرست خالی مجوزها هم پوشش‌نشده است.
/// </para>
/// <para>
/// <b>ابهام هرگز حل نمی‌شود.</b> اگر بیش از یک سیاست هم‌سطح و ناسازگار دامنهٔ درخواست را بپوشاند،
/// نتیجه <see cref="ShippingServiceabilityOutcome.ScopeConflict"/> است و سیاستی انتخاب نمی‌شود؛
/// «اولین سطر برنده» یک تصمیم تجاری پنهان است که این لایه اجازهٔ ساختنش را ندارد.
/// </para>
/// <para>
/// <b>اصل «عمیق‌ترین دامنه برنده است».</b> بین سیاست‌های سازگار، عمیق‌ترین سطح دامنه انتخاب
/// می‌شود تا نرخ شهری بتواند نرخ استانی را کنار بزند؛ این قاعده فقط ترتیب را تعیین می‌کند و
/// هرگز پوشش را ایجاد نمی‌کند.
/// </para>
/// </remarks>
public static class ShippingRatePolicyEngine
{
    /// <summary>نرخ و خدمت‌رسانی یک درخواست را محاسبه می‌کند.</summary>
    /// <param name="request">درخواست نرخ.</param>
    /// <param name="policies">سیاست‌های نرخ؛ ممکن است خالی باشد.</param>
    /// <param name="coverageGrants">مجوزهای پوشش؛ ممکن است خالی باشد (که یعنی پوشش‌نشده).</param>
    /// <returns>نتیجهٔ قطعی؛ هرگز null.</returns>
    public static ShippingQuote Quote(
        ShippingQuoteRequest request,
        IReadOnlyCollection<ShippingRatePolicy>? policies,
        IReadOnlyCollection<ShippingCoverageGrant>? coverageGrants)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var destination = new ShippingDestinationScope(
            request.Destination.CountryId,
            request.Destination.AdministrativeAreaId,
            request.Destination.CityId,
            request.Destination.ServiceAreaId);

        var candidates = (policies ?? Array.Empty<ShippingRatePolicy>())
            .Where(x => x.IsActive)
            .Where(x => string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Covers(destination))
            .ToArray();

        if (candidates.Length == 0)
        {
            return NotQuoted(request, serviceCode, ShippingServiceabilityOutcome.UnknownService, ShippingReasonCodes.ServiceNotPriced);
        }

        var deepest = candidates.Max(x => x.Scope.Level);
        var mostSpecific = candidates.Where(x => x.Scope.Level == deepest).ToArray();
        if (mostSpecific.Length > 1)
        {
            // Two equally specific policies that both cover the request. Even when they carry the same
            // amount, the pair is a data conflict that must be resolved by a human, not by row order.
            return NotQuoted(request, serviceCode, ShippingServiceabilityOutcome.ScopeConflict, ShippingReasonCodes.ScopeAmbiguous);
        }

        var policy = mostSpecific[0];

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode)
            && !string.Equals(policy.CurrencyCode, request.CurrencyCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return NotQuoted(request, serviceCode, ShippingServiceabilityOutcome.CurrencyMismatch, ShippingReasonCodes.CurrencyMismatch);
        }

        var covered = (coverageGrants ?? Array.Empty<ShippingCoverageGrant>())
            .Any(x => x.IsActive
                && string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase)
                && x.Covers(destination));
        if (!covered)
        {
            return NotQuoted(request, serviceCode, ShippingServiceabilityOutcome.NotCovered, ShippingReasonCodes.CoverageMissing);
        }

        var freeByThreshold = policy.FreeAboveThreshold is { } threshold
            && request.CartSubtotalExclusive >= threshold;
        var charge = freeByThreshold ? 0m : policy.ChargeAmount;

        return new ShippingQuote(
            ShippingServiceabilityOutcome.Quoted,
            serviceCode,
            charge,
            policy.CurrencyCode,
            policy.PreparationDays,
            policy.LeadDays,
            policy.FreeAboveThreshold,
            freeByThreshold ? ShippingReasonCodes.QuotedFreeAboveThreshold : ShippingReasonCodes.Quoted);
    }

    private static ShippingQuote NotQuoted(
        ShippingQuoteRequest request,
        string serviceCode,
        ShippingServiceabilityOutcome outcome,
        string reasonCode)
        => new(outcome, serviceCode, 0m, null, 0, 0, null, reasonCode);
}
