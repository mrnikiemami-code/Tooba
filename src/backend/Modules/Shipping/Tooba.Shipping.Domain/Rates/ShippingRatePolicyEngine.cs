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
/// <para>
/// <b>مالکیت سخت‌گیرانهٔ گزینه (اختیاری، پیش‌فرض خاموش).</b>
/// <see cref="ShippingQuoteRequest.OptionMatchMode"/> می‌تواند
/// <see cref="ShippingOptionMatchMode.StrictOwnedOption"/> باشد؛ در آن حالت فقط ردیفی واجد شرایط است
/// که گزینهٔ خودش دقیقاً با گزینهٔ درخواست برابر باشد و هیچ fallback سطح‌خدمت/هم‌خانواده‌ای وجود
/// ندارد. این انتخاب <em>صریح</em> است: در حالت پیش‌فرض (<see cref="ShippingOptionMatchMode.LegacyFallback"/>)
/// رفتار بیت‌به‌بیت همان قبل می‌ماند تا خدمت‌های تک‌گزینه‌ای که فقط ردیف سطح‌خدمت دارند نشکنند.
/// </para>
/// </remarks>
public static class ShippingRatePolicyEngine
{
    /// <summary>نرخ و خدمت‌رسانی یک درخواست را محاسبه می‌کند.</summary>
    /// <param name="request">درخواست نرخ.</param>
    /// <param name="policies">سیاست‌های نرخ؛ ممکن است خالی باشد.</param>
    /// <param name="coverageGrants">مجوزهای پوشش؛ ممکن است خالی باشد (که یعنی پوشش‌نشده).</param>
    /// <returns>نتیجهٔ قطعی؛ هرگز null.</returns>
    /// <remarks>
    /// این overload برای فراخوان‌های موجود دست‌نخورده باقی می‌ماند: هیچ اعلام حالت تحدیدی وجود ندارد،
    /// پس مسیر legacy و مسیر StrictOwnedOption بدون اعلام دقیقاً مانند قبل رفتار می‌کنند.
    /// </remarks>
    public static ShippingQuote Quote(
        ShippingQuoteRequest request,
        IReadOnlyCollection<ShippingRatePolicy>? policies,
        IReadOnlyCollection<ShippingCoverageGrant>? coverageGrants)
        => Quote(request, policies, coverageGrants, null);

    /// <summary>
    /// نرخ و خدمت‌رسانی یک درخواست را با در نظر گرفتن اعلام‌های حالت تحدید روش محاسبه می‌کند.
    /// </summary>
    /// <param name="request">درخواست نرخ.</param>
    /// <param name="policies">سیاست‌های نرخ؛ ممکن است خالی باشد.</param>
    /// <param name="coverageGrants">مجوزهای پوشش، شامل ردیف‌های سطح‌خدمت و گزینه‌دار.</param>
    /// <param name="methodCoverages">
    /// اعلام‌های حالت تحدید روش‌ها؛ تهی/خالی یعنی هیچ محدودیت ادمین‌تعریف‌شده‌ای وجود ندارد.
    /// </param>
    /// <returns>نتیجهٔ قطعی؛ هرگز null.</returns>
    public static ShippingQuote Quote(
        ShippingQuoteRequest request,
        IReadOnlyCollection<ShippingRatePolicy>? policies,
        IReadOnlyCollection<ShippingCoverageGrant>? coverageGrants,
        IReadOnlyCollection<ShippingMethodCoverage>? methodCoverages)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var optionCode = request.OptionCode?.Trim();
        var strictOwnedOption = request.OptionMatchMode == ShippingOptionMatchMode.StrictOwnedOption;
        var destination = new ShippingDestinationScope(
            request.Destination.CountryId,
            request.Destination.AdministrativeAreaId,
            request.Destination.CityId,
            request.Destination.ServiceAreaId);

        var active = (policies ?? Array.Empty<ShippingRatePolicy>())
            .Where(x => x.IsActive)
            .Where(x => string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var covering = active
            .Where(x => x.Covers(destination))
            .Where(x => strictOwnedOption
                ? MatchesOwnedOptionExactly(x, optionCode)
                : x.MatchesOption(optionCode))
            .ToArray();

        if (covering.Length == 0)
        {
            // Strict mode never falls back to a service-level/provider row or a sibling option. When the
            // service itself has active rows that were excluded purely by option ownership, that is a
            // distinct, stable fact ("this option is not priced") and not a generic unknown service.
            var reason = strictOwnedOption
                && optionCode is not null
                && active.Any(x => x.Covers(destination))
                    ? ShippingReasonCodes.OwnedOptionNotPriced
                    : ShippingReasonCodes.ServiceNotPriced;
            return NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.UnknownService, reason);
        }

        // Option identity is resolved inside the winning tier only: the request's own option-specific
        // tier when one exists, otherwise the documented service-level fallback tier. A service-level
        // policy therefore keeps working unchanged for single-option and no-option services.
        // In StrictOwnedOption mode every remaining candidate already shares one exact option identity,
        // so this precedence step is a no-op there and cannot reintroduce borrowing.
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

        // Coverage is an eligibility decision, separate from pricing readiness.
        //
        // Legacy path (default): one service-level grant authorizes the whole service at that destination,
        // including every option and every tier. Rows that carry an option identity are deliberately NOT
        // consulted here, so adding option-scoped rows can never widen or narrow legacy coverage.
        //
        // Strict path: only the request's OWN method declares its geographic restriction. No active
        // declaration (or an explicit Unrestricted one) means "Admin defined no restriction" and is NOT a
        // coverage failure; the owned rate above is still mandatory. A Restricted declaration is enforced
        // against the grants owned by that exact method, and an empty allowed set denies every destination
        // instead of silently becoming "unrestricted". Provider/service-level grants never authorize a
        // restricted method.
        var grants = (coverageGrants ?? Array.Empty<ShippingCoverageGrant>())
            .Where(x => x.IsActive)
            .Where(x => string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        bool covered;
        string coverageReason = ShippingReasonCodes.CoverageMissing;
        if (!strictOwnedOption)
        {
            covered = grants.Any(x => x.OptionCode is null && x.Covers(destination));
        }
        else
        {
            var declaration = (methodCoverages ?? Array.Empty<ShippingMethodCoverage>())
                .FirstOrDefault(x => x.BelongsToMethod(serviceCode, optionCode));

            covered = declaration is null || !declaration.IsRestrictive
                || grants.Any(x => x.BelongsToMethod(serviceCode, optionCode) && x.Covers(destination));
            if (!covered)
            {
                coverageReason = ShippingReasonCodes.CoverageMethodRestricted;
            }
        }

        if (!covered)
        {
            return NotQuoted(request, optionCode, serviceCode, ShippingServiceabilityOutcome.NotCovered, coverageReason);
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

    /// <summary>
    /// تطبیق مالکیت سخت‌گیرانهٔ گزینه: فقط ردیفی که گزینهٔ <em>خودش</em> دقیقاً با گزینهٔ درخواست
    /// برابر است. ردیف سطح‌خدمت (<c>OptionCode</c> تهی) و ردیف گزینهٔ هم‌خانواده هرگز واجد شرایط
    /// نیستند، پس «اولین تطابق» یا fallback ضمنی وجود ندارد.
    /// </summary>
    /// <param name="policy">سیاست نامزد.</param>
    /// <param name="optionCode">گزینهٔ نرمال‌شدهٔ درخواست.</param>
    private static bool MatchesOwnedOptionExactly(ShippingRatePolicy policy, string? optionCode)
        => string.Equals(policy.OptionCode, optionCode, StringComparison.OrdinalIgnoreCase);
}
