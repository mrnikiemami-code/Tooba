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
/// <b>هویت گزینه پیش از عمق دامنه.</b> وقتی هم سیاست گزینه‌دار و هم سیاست سطح‌خدمت با دامنهٔ
/// درخواست سازگارند، سیاست گزینه‌دار برنده است: فراخوان صریحاً همان گزینه را پرسیده و نادیده
/// گرفتن آن یک تصمیم تجاری پنهان است. در نبود سیاست گزینه‌دار، سیاست سطح‌خدمت به‌عنوان یک
/// <em>fallback</em> صریح به کار می‌آید تا خدمت‌های تک‌گزینه‌ای دست‌نخورده بمانند. یک سیاست
/// گزینه‌دار هرگز روی گزینهٔ دیگری اثر نمی‌گذارد.
/// </para>
/// <para>
/// <b>اصل «عمیق‌ترین دامنه برنده است».</b> بین سیاست‌های سازگار، عمیق‌ترین سطح دامنه انتخاب
/// می‌شود تا نرخ شهری بتواند نرخ استانی را کنار بزند؛ این قاعده فقط ترتیب را تعیین می‌کند و
/// هرگز پوشش را ایجاد نمی‌کند.
/// </para>
/// <para>
/// <b>پلهٔ اندازه آخرین لایه است، نه اولی.</b> شرط وزن/تعداد فقط <em>داخل</em> هویت برنده
/// (گزینه + عمیق‌ترین دامنه) تفکیک می‌کند: یک پلهٔ وزنی شهر هرگز نرخ استانی را کنار نمی‌زند و
/// یک پلهٔ وزنی گزینهٔ express هرگز روی standard اثر نمی‌گذارد. ترتیب نهایی قطعی است:
/// <c>هویت گزینه → عمق دامنه → تطبیق پلهٔ اندازه</c>.
/// </para>
/// <para>
/// <b>پلهٔ محدود هرگز با اندازهٔ غایب تطبیق نمی‌یابد.</b> سیاست شرط‌دار فقط وقتی قیمت می‌دهد که
/// فراخوان اندازهٔ همان محور را اعلام کرده باشد و مقدار داخل بازهٔ نیم‌باز بیفتد. سیاست بی‌شرط
/// (ردیف قدیمی) رفتارش دقیقاً مانند قبل است و به‌عنوان قاعدهٔ پایه در همان هویت باقی می‌ماند.
/// </para>
/// <para>
/// <b>ابهام پله‌ای هم حل نمی‌شود.</b> اگر بیش از یک پلهٔ شرط‌دار با اندازهٔ درخواست سازگار باشد،
/// نتیجه <see cref="ShippingServiceabilityOutcome.ConditionConflict"/> است و سیاستی انتخاب نمی‌شود؛
/// «اولین سطر برنده» یک تصمیم تجاری پنهان است.
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
        var optionCode = request.OptionCode?.Trim();
        var destination = new ShippingDestinationScope(
            request.Destination.CountryId,
            request.Destination.AdministrativeAreaId,
            request.Destination.CityId,
            request.Destination.ServiceAreaId);

        var covering = (policies ?? Array.Empty<ShippingRatePolicy>())
            .Where(x => x.IsActive)
            .Where(x => string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Covers(destination))
            .Where(x => x.MatchesOption(optionCode))
            .ToArray();

        if (covering.Length == 0)
        {
            return NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.UnknownService, ShippingReasonCodes.ServiceNotPriced);
        }

        // Option identity is resolved inside the winning tier only: the request's own option-specific
        // tier when one exists, otherwise the documented service-level fallback tier. A service-level
        // policy therefore keeps working unchanged for single-option and no-option services.
        var candidates = covering.Any(x => x.IsOptionSpecific)
            ? covering.Where(x => x.IsOptionSpecific).ToArray()
            : covering;

        var deepest = candidates.Max(x => x.Scope.Level);
        var group = candidates.Where(x => x.Scope.Level == deepest).ToArray();

        // Measurement tiers are a refinement INSIDE the winning identity+scope group, so the group is
        // resolved first and the tier second: a weight tier never overrides option or destination
        // precedence, it only decides between sibling rows that already share both.
        var unconditioned = group.Where(x => x.IsUnconditioned).ToArray();
        var conditioned = group.Where(x => !x.IsUnconditioned).ToArray();

        if (unconditioned.Length > 1)
        {
            // Two equally specific unconditioned policies that both cover the request. Even when they
            // carry the same amount, the pair is a data conflict that must be resolved by a human.
            return NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.ScopeConflict, ShippingReasonCodes.ScopeAmbiguous);
        }

        ShippingRatePolicy policy;
        ShippingConditionMatch? matchedCondition = null;

        if (conditioned.Length == 0)
        {
            policy = unconditioned[0];
        }
        else
        {
            var matched = conditioned.Where(x => x.MatchesCondition(request.Parcel)).ToArray();
            if (matched.Length > 1)
            {
                return NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.ConditionConflict, ShippingReasonCodes.ConditionAmbiguous);
            }

            if (matched.Length == 1)
            {
                policy = matched[0];
                matchedCondition = new ShippingConditionMatch(
                    policy.Condition.Weight,
                    policy.Condition.Count);
            }
            else if (unconditioned.Length == 1)
            {
                // Documented base-rate fallback: a conditioned sibling is an explicit exception and the
                // unconditioned row stays the general rule for measurements it does not cover.
                policy = unconditioned[0];
            }
            else
            {
                // No sibling matched and there is no base rate to fall back to. A request that simply
                // omitted a required measurement is reported differently from one that supplied a
                // measurement outside every declared tier: "we cannot price this without a size" is a
                // different fact from "this size is not in any tier".
                var missingMeasurement = request.Parcel is null
                    || conditioned.Any(x => x.HasUnsuppliedAxis(request.Parcel));
                return missingMeasurement
                    ? NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.MeasurementRequired, ShippingReasonCodes.MeasurementRequired)
                    : NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.ConditionNotMatched, ShippingReasonCodes.ConditionNotMatched);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode)
            && !string.Equals(policy.CurrencyCode, request.CurrencyCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.CurrencyMismatch, ShippingReasonCodes.CurrencyMismatch);
        }

        // Coverage stays a service+destination decision: one grant authorizes the whole service at that
        // destination, including every option and every weight/count tier of it, and it never widens scope.
        var covered = (coverageGrants ?? Array.Empty<ShippingCoverageGrant>())
            .Any(x => x.IsActive
                && string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase)
                && x.Covers(destination));
        if (!covered)
        {
            return NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.NotCovered, ShippingReasonCodes.CoverageMissing);
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
            freeByThreshold ? ShippingReasonCodes.QuotedFreeAboveThreshold : ShippingReasonCodes.Quoted,
            optionCode,
            matchedCondition);
    }

    private static ShippingQuote NotQuoted(
        ShippingQuoteRequest request,
        string? optionCode,
        string serviceCode,
        ShippingServiceabilityOutcome outcome,
        string reasonCode)
        => new(outcome, serviceCode, 0m, null, 0, 0, null, reasonCode, optionCode);
}
