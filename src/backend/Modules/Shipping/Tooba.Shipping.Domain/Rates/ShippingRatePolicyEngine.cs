using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Authoring;

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
    /// قیمت‌گذاری یک بازبینیِ <b>capture‌شدهٔ</b> با ارزیابی سه‌محوری صریح (وزن/تعداد/مبلغ کالا).
    /// </summary>
    /// <param name="request">ورودی صریح و مورداعتماد سرور (هویت، payload رمزگشایی‌شده، مسیر، اندازه‌ها، ارز).</param>
    /// <returns>نتیجهٔ قطعی؛ هرگز null.</returns>
    /// <remarks>
    /// <para>
    /// <b>یک ردیف، نه جمع جایگزین‌ها.</b> هر ردیف ذخیره‌شده با هر سه محور به‌صورت <c>AND</c> سنجیده
    /// می‌شود؛ محورِ ثبت‌نشده بی‌شرط است و محورِ ثبت‌شدهٔ بی‌اندازه قیمت نمی‌دهد. دقیقاً یک ردیف برنده
    /// می‌شود؛ صفر ردیف «قیمت‌نشده» و بیش از یک ردیف «مبهم» است. هیچ fallback نرخ پایه، هیچ
    /// «اولین/ارزان‌ترین/عمیق‌ترین» و هیچ جمع‌کردن جایگزین‌ها وجود ندارد.
    /// </para>
    /// <para>
    /// <b>یک ردیف، دو مؤلفه.</b> کرایه و بسته‌بندی از همان ردیف برنده خوانده می‌شوند؛ resolver
    /// بسته‌بندیِ scope-only اینجا به کار نمی‌آید چون هویت ردیف تطبیق‌یافته را از دست می‌دهد.
    /// </para>
    /// <para>
    /// <b>هیچ صفر موفق‌نما.</b> نبود تنظیمات، حالت CarrierApi/Free، نبود پوشش/ارز/اندازه یا دادهٔ
    /// ناسازگار همه باعث می‌شوند <em>همهٔ</em> مبالغ <c>null</c> بمانند.
    /// </para>
    /// <para>
    /// <b>فقط پولی.</b> این متد هیچ DB/Geography/ساعت نمی‌خواند و هیچ ادعای فعال‌سازی/صلاحیت مشتری یا
    /// آمادگی عملیاتی نمی‌سازد؛ نتیجهٔ پولی به‌تنهایی «روش برای مشتری فعال است» نیست.
    /// </para>
    /// </remarks>
    public static ShippingCapturedRevisionQuote QuoteCapturedRevision(
        ShippingCapturedRevisionQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CapturedRevision);
        ArgumentNullException.ThrowIfNull(request.Expected);

        var payload = request.CapturedRevision;
        var serviceCode = payload.ServiceCode?.Trim() ?? string.Empty;
        var optionCode = string.IsNullOrWhiteSpace(payload.OptionCode) ? null : payload.OptionCode.Trim();

        // Identity is checked BEFORE any amount: a payload that describes another owner/draft/revision is
        // never priced, even if it happens to be shaped correctly.
        if (!IdentityMatches(request.Expected, payload))
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.IdentityMismatch,
                ShippingCapturedQuoteReasonCodes.IdentityMismatch,
                serviceCode,
                optionCode);
        }

        // "Not configured" is a stored fact, not a default: a method without captured settings cannot price.
        if (payload.MethodSettings is not { } settings)
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.NotConfigured,
                ShippingCapturedQuoteReasonCodes.NotConfigured,
                serviceCode,
                optionCode);
        }

        if (!Enum.IsDefined(settings.CalculationMode) || !Enum.IsDefined(settings.FreightPaymentMode))
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.ConfigurationInvalid,
                ShippingCapturedQuoteReasonCodes.ConfigurationInvalid,
                serviceCode,
                optionCode);
        }

        switch (settings.CalculationMode)
        {
            case ShippingAuthoringCalculationMode.CarrierApi:
                return NotQuoted(
                    ShippingCapturedQuoteOutcome.CalculationModeUnavailable,
                    ShippingCapturedQuoteReasonCodes.CarrierModeUnavailable,
                    serviceCode,
                    optionCode);
            case ShippingAuthoringCalculationMode.Free:
                // Freezing the real expense to zero would fabricate a free service; the funder/provider is
                // future authority, so this is an explicit not-ready answer today.
                return NotQuoted(
                    ShippingCapturedQuoteOutcome.CalculationModeUnavailable,
                    ShippingCapturedQuoteReasonCodes.FreeNotImplemented,
                    serviceCode,
                    optionCode);
            case ShippingAuthoringCalculationMode.Manual:
                break;
            default:
                return NotQuoted(
                    ShippingCapturedQuoteOutcome.ConfigurationInvalid,
                    ShippingCapturedQuoteReasonCodes.ConfigurationInvalid,
                    serviceCode,
                    optionCode);
        }

        // Measurements/amount are caller-declared facts. A negative one is invalid input, never a zero rate.
        if (request.GroupWeightGrams is < 0
            || request.GroupItemCount is < 0
            || request.FinalGroupGoodsAmountAfterDiscountAndTax is < 0m)
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.ConfigurationInvalid,
                ShippingCapturedQuoteReasonCodes.ConfigurationInvalid,
                serviceCode,
                optionCode);
        }

        if (!IsStructurallyValidDestination(request.Destination))
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.DestinationPathInvalid,
                ShippingCapturedQuoteReasonCodes.DestinationPathInvalid,
                serviceCode,
                optionCode);
        }

        if (payload.Groups is null)
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.ConfigurationInvalid,
                ShippingCapturedQuoteReasonCodes.ConfigurationInvalid,
                serviceCode,
                optionCode);
        }

        // Several matched AREAS in the SAME group are still ONE group: geography is not an actor and does
        // not multiply choices. Only several GROUPS make the request ambiguous.
        var matchedGroups = payload.Groups
            .Where(group => group.Areas is not null
                            && group.Areas.Any(area => AreaMatches(area, request.Destination)))
            .ToArray();

        if (matchedGroups.Length == 0)
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.CoverageMissing,
                ShippingCapturedQuoteReasonCodes.CoverageMissing,
                serviceCode,
                optionCode);
        }

        if (matchedGroups.Length > 1)
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.GroupAmbiguous,
                ShippingCapturedQuoteReasonCodes.GroupAmbiguous,
                serviceCode,
                optionCode);
        }

        var group = matchedGroups[0];
        var groupCurrency = NormalizeCurrency(group.CurrencyCode);
        var requestCurrency = NormalizeCurrency(request.CurrencyCode);
        if (groupCurrency is null || requestCurrency is null)
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.CurrencyMissing,
                ShippingCapturedQuoteReasonCodes.CurrencyMissing,
                serviceCode,
                optionCode);
        }

        if (!string.Equals(groupCurrency, requestCurrency, StringComparison.Ordinal))
        {
            // No FX is invented: another currency is a not-quoted answer, not a converted amount.
            return NotQuoted(
                ShippingCapturedQuoteOutcome.CurrencyMismatch,
                ShippingCapturedQuoteReasonCodes.CurrencyMismatch,
                serviceCode,
                optionCode);
        }

        if (group.RateRows is null || group.RateRows.Count == 0)
        {
            return NotQuoted(
                ShippingCapturedQuoteOutcome.RateRowMissing,
                ShippingCapturedQuoteReasonCodes.RateRowMissing,
                serviceCode,
                optionCode);
        }

        ShippingAuthoringRevisionRateRow? winner = null;
        ShippingNumericTier? winnerWeight = null;
        ShippingNumericTier? winnerCount = null;
        ShippingAmountTier? winnerMerchandise = null;
        var matches = 0;
        var undetermined = 0;

        foreach (var row in group.RateRows)
        {
            if (!TryBuildCondition(row, out var weight, out var count, out var merchandise))
            {
                // One incoherent stored bound invalidates the money answer for this group: bounds are
                // facts, and normalising them here would silently invent a tier.
                return NotQuoted(
                    ShippingCapturedQuoteOutcome.ConfigurationInvalid,
                    ShippingCapturedQuoteReasonCodes.ConfigurationInvalid,
                    serviceCode,
                    optionCode);
            }

            var weightMatch = AxisMatch(weight, request.GroupWeightGrams);
            var countMatch = AxisMatch(count, request.GroupItemCount);
            var merchandiseMatch = MerchandiseMatch(merchandise, request.FinalGroupGoodsAmountAfterDiscountAndTax);

            if (weightMatch == CapturedAxisMatch.Outside
                || countMatch == CapturedAxisMatch.Outside
                || merchandiseMatch == CapturedAxisMatch.Outside)
            {
                // A KNOWN axis that does not match excludes the row outright, before any other axis is
                // allowed to make it "undetermined" — an irrelevant row never blocks a valid one.
                continue;
            }

            if (weightMatch == CapturedAxisMatch.MissingMeasurement
                || countMatch == CapturedAxisMatch.MissingMeasurement
                || merchandiseMatch == CapturedAxisMatch.MissingMeasurement)
            {
                undetermined++;
                continue;
            }

            matches++;
            winner = row;
            winnerWeight = weight;
            winnerCount = count;
            winnerMerchandise = merchandise;
        }

        if (matches > 1)
        {
            // Even with identical amounts, two matching alternatives are a data ambiguity a human resolves.
            return NotQuoted(
                ShippingCapturedQuoteOutcome.ConditionAmbiguous,
                ShippingCapturedQuoteReasonCodes.ConditionAmbiguous,
                serviceCode,
                optionCode);
        }

        if (matches == 0)
        {
            return undetermined > 0
                ? NotQuoted(
                    ShippingCapturedQuoteOutcome.MeasurementRequired,
                    ShippingCapturedQuoteReasonCodes.MeasurementRequired,
                    serviceCode,
                    optionCode)
                : NotQuoted(
                    ShippingCapturedQuoteOutcome.ConditionNotMatched,
                    ShippingCapturedQuoteReasonCodes.ConditionNotMatched,
                    serviceCode,
                    optionCode);
        }

        // Exactly one row won: BOTH components come from it, so a mixed total is impossible.
        var freightAmount = winner!.FreightAmount;
        var packagingAmount = winner.PackagingAmount;
        var total = freightAmount + packagingAmount;

        // The module's defined payer semantics: packaging is always a checkout charge (the carrier has no
        // packaging contract), so only the freight component can move to collect-on-delivery.
        var carrierCollect = settings.FreightPaymentMode == ShippingFreightPayerMode.CollectOnDelivery
            ? freightAmount
            : 0m;
        var payableAtCheckout = total - carrierCollect;

        return new ShippingCapturedRevisionQuote(
            ShippingCapturedQuoteOutcome.Quoted,
            ShippingCapturedQuoteReasonCodes.Quoted,
            serviceCode,
            optionCode,
            groupCurrency,
            freightAmount,
            packagingAmount,
            total,
            payableAtCheckout,
            carrierCollect,
            settings.FreightPaymentMode,
            new ShippingCapturedQuoteSelection(
                group.GroupId,
                winner.RateRowId,
                payload.DraftId,
                payload.SourceRevision,
                payload.FormatVersion,
                payload.ShippingTypeId,
                winner.MinDeliveryDays,
                winner.MaxDeliveryDays,
                settings,
                payload.WeeklySchedule,
                new ShippingCapturedQuoteMatchedConditions(winnerWeight, winnerCount, winnerMerchandise)));
    }

    private static ShippingCapturedRevisionQuote NotQuoted(
        ShippingCapturedQuoteOutcome outcome,
        string reasonCode,
        string serviceCode,
        string? optionCode)
        => new(
            outcome,
            reasonCode,
            serviceCode,
            optionCode,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    /// <summary>
    /// تطبیق مالکیت سخت‌گیرانهٔ گزینه: فقط ردیفی که گزینهٔ <em>خودش</em> دقیقاً با گزینهٔ درخواست
    /// برابر است. ردیف سطح‌خدمت (<c>OptionCode</c> تهی) و ردیف گزینهٔ هم‌خانواده هرگز واجد شرایط
    /// نیستند، پس «اولین تطابق» یا fallback ضمنی وجود ندارد.
    /// </summary>
    /// <param name="policy">سیاست نامزد.</param>
    /// <param name="optionCode">گزینهٔ نرمال‌شدهٔ درخواست.</param>
    private static bool MatchesOwnedOptionExactly(ShippingRatePolicy policy, string? optionCode)
        => string.Equals(policy.OptionCode, optionCode, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ captured-revision quote helpers

    /// <summary>نتیجهٔ سنجش یک محور روی یک ردیف.</summary>
    private enum CapturedAxisMatch
    {
        /// <summary>محور ثبت نشده یا اندازهٔ اعلام‌شده داخل بازه است.</summary>
        Matched,

        /// <summary>محور ثبت شده ولی اندازهٔ آن اعلام نشده است؛ صفر فرض نمی‌شود.</summary>
        MissingMeasurement,

        /// <summary>اندازهٔ اعلام‌شده بیرون بازهٔ ثبت‌شده است.</summary>
        Outside,
    }

    /// <summary>هویت موردانتظار باید عیناً با هدر/بدنهٔ capture یکی باشد؛ هیچ دادهٔ غریبه قیمت نمی‌گیرد.</summary>
    /// <param name="expected">هویت موردانتظار.</param>
    /// <param name="payload">بدنهٔ capture‌شده.</param>
    private static bool IdentityMatches(
        ShippingCapturedRevisionIdentity expected,
        ShippingAuthoringRevisionPayload payload)
        => payload.Owner is not null
           && payload.Owner.Kind == expected.OwnerKind
           && string.Equals(payload.Owner.TenantId, expected.OwnerTenantId, StringComparison.Ordinal)
           && payload.Owner.SellerPartyId == expected.OwnerSellerPartyId
           && payload.DraftId == expected.DraftId
           && payload.SourceRevision == expected.SourceRevision;

    /// <summary>سنجش محور اندازهٔ صحیح با بازهٔ نیم‌باز موجود.</summary>
    /// <param name="tier">بازهٔ ثبت‌شده یا تهی (بی‌شرط).</param>
    /// <param name="value">اندازهٔ اعلام‌شده یا تهی.</param>
    private static CapturedAxisMatch AxisMatch(ShippingNumericTier? tier, long? value)
    {
        if (tier is null)
        {
            return CapturedAxisMatch.Matched;
        }

        return value is { } measured
            ? tier.Contains(measured) ? CapturedAxisMatch.Matched : CapturedAxisMatch.Outside
            : CapturedAxisMatch.MissingMeasurement;
    }

    /// <summary>سنجش محور مبلغ کالا با همان معنای نیم‌باز.</summary>
    /// <param name="tier">بازهٔ ثبت‌شده یا تهی (بی‌شرط).</param>
    /// <param name="value">مبلغ نهایی اعلام‌شده یا تهی.</param>
    private static CapturedAxisMatch MerchandiseMatch(ShippingAmountTier? tier, decimal? value)
    {
        if (tier is null)
        {
            return CapturedAxisMatch.Matched;
        }

        return value is { } measured
            ? tier.Contains(measured) ? CapturedAxisMatch.Matched : CapturedAxisMatch.Outside
            : CapturedAxisMatch.MissingMeasurement;
    }

    /// <summary>
    /// بازه‌های سه‌محوری یک ردیف را می‌سازد و کران/مبلغ نامعتبر را رد می‌کند. «تهی» یعنی محور بی‌شرط؛
    /// کرانِ بالای بدون کران پایین، کرانِ چسبیده/معکوس، مبلغ منفی و روزهای نامعتبر همه دادهٔ ناسازگارند.
    /// </summary>
    /// <param name="row">ردیف ذخیره‌شده.</param>
    /// <param name="weight">بازهٔ وزن یا تهی.</param>
    /// <param name="count">بازهٔ تعداد یا تهی.</param>
    /// <param name="merchandise">بازهٔ مبلغ کالا یا تهی.</param>
    private static bool TryBuildCondition(
        ShippingAuthoringRevisionRateRow row,
        out ShippingNumericTier? weight,
        out ShippingNumericTier? count,
        out ShippingAmountTier? merchandise)
    {
        weight = null;
        count = null;
        merchandise = null;

        if (row.MinWeightGrams is { } minWeight)
        {
            if (minWeight < 0 || (row.MaxWeightGrams is { } maxWeight && maxWeight <= minWeight))
            {
                return false;
            }

            weight = ShippingNumericTier.Between(minWeight, row.MaxWeightGrams);
        }
        else if (row.MaxWeightGrams is not null)
        {
            return false;
        }

        if (row.MinItemCount is { } minCount)
        {
            if (minCount < 0 || (row.MaxItemCount is { } maxCount && maxCount <= minCount))
            {
                return false;
            }

            count = ShippingNumericTier.Between(minCount, row.MaxItemCount);
        }
        else if (row.MaxItemCount is not null)
        {
            return false;
        }

        if (row.MinMerchandiseAmount is { } minAmount)
        {
            if (minAmount < 0m || (row.MaxMerchandiseAmount is { } maxAmount && maxAmount <= minAmount))
            {
                return false;
            }

            merchandise = ShippingAmountTier.Between(minAmount, row.MaxMerchandiseAmount);
        }
        else if (row.MaxMerchandiseAmount is not null)
        {
            return false;
        }

        return row.FreightAmount >= 0m
               && row.PackagingAmount >= 0m
               && row.MinDeliveryDays >= 0
               && row.MaxDeliveryDays >= row.MinDeliveryDays;
    }

    /// <summary>
    /// یک انتخاب جغرافیایی ذخیره‌شده با همان گونه و شناسهٔ مسیر تأییدشدهٔ مقصد تطبیق می‌یابد.
    /// </summary>
    /// <param name="area">انتخاب ذخیره‌شده.</param>
    /// <param name="path">مسیر تایپ‌شدهٔ مقصد.</param>
    private static bool AreaMatches(ShippingAuthoringRevisionArea area, ShippingDestinationScope path)
        => area.NodeKind switch
        {
            ShippingDestinationNodeKind.Country => path.CountryId == area.NodeId,
            ShippingDestinationNodeKind.AdministrativeArea => path.AdministrativeAreaId == area.NodeId,
            ShippingDestinationNodeKind.City => path.CityId == area.NodeId,
            ShippingDestinationNodeKind.ServiceArea => path.ServiceAreaId == area.NodeId,
            _ => false,
        };

    /// <summary>
    /// ساختار مسیر مقصد را می‌سنجد: کشور الزامی، هیچ شناسهٔ تهی، و ناحیهٔ خدمت بدون شهر یک مسیر
    /// نیمه‌دامنه است. این اعتبارسنجیِ وجودِ واقعی گره در Geography نیست؛ آن کار آداپتر آینده است.
    /// </summary>
    /// <param name="path">مسیر مقصد.</param>
    private static bool IsStructurallyValidDestination(ShippingDestinationScope path)
        => path is not null
           && path.CountryId != Guid.Empty
           && path.AdministrativeAreaId != Guid.Empty
           && path.CityId != Guid.Empty
           && path.ServiceAreaId != Guid.Empty
           && (path.ServiceAreaId is null || path.CityId is not null);

    private static string? NormalizeCurrency(string? currencyCode)
    {
        var trimmed = currencyCode?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
