using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;

/// <summary>
/// هستهٔ <b>بدون خوانندهٔ فکت</b> ترکیب گروه/نامزد (TB-233-R2): همان اعتبارسنجی برنامه/فکت، همان
/// یک خواندن ساعت سرور، همان یک نقل‌قول ۲۱۶ و همان تقویم ۲۱۳ برای هر نامزد.
/// </summary>
/// <remarks>
/// <para>
/// این کلاس هیچ ارجاعی به <see cref="ICheckoutShippingGroupGoodsFactsReader"/>، Preview/Submit یا
/// <c>CheckoutDirectory</c> ندارد؛ به همین دلیل <c>CheckoutDirectory</c> می‌تواند همین هسته را در
/// همان scope تزریق کند بدون چرخهٔ سازنده. پوشش ۲۱۸ فقط فکت‌ها را می‌خواند و به همین هسته می‌دهد؛
/// مسیر فکت‌های آمادهٔ submit (خطوط واقعی <c>SellerOrder</c>) هم دقیقاً همین هسته را صدا می‌زند.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedGroupCompositionCore(
    IShippingCapturedRevisionQuoteLookup capturedQuoteLookup,
    IClock clock)
{
    /// <summary>
    /// برنامهٔ گروه/نامزد را <b>پیش از هر خواندن</b> اعتبارسنجی می‌کند تا یک برنامهٔ ناسازگار هیچ
    /// خواندن سبد/فکت/قیمت ایجاد نکند.
    /// </summary>
    /// <param name="groups">برنامهٔ گروه/نامزد.</param>
    public void EnsurePlanIsValid(IReadOnlyList<StorefrontCapturedGroupPlan>? groups) => ValidatePlan(groups);

    /// <summary>
    /// فکت‌های آماده را اعتبارسنجی و سپس برای هر نامزد یک قیمت/تقویم می‌سازد. فکت‌ها باید همان
    /// <b>سبد/نسخهٔ فرمان</b> باشند، نه سبدی که خودشان ادعا می‌کنند.
    /// </summary>
    /// <param name="command">فرمان مورداعتماد سرور؛ سبد/نسخهٔ معتبر از همین می‌آید.</param>
    /// <param name="groups">برنامهٔ گروه/نامزد.</param>
    /// <param name="facts">فکت‌های ساخته‌شده از منبع Order.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <param name="requirePlannedDispatch">
    /// <c>true</c> برای <b>دسترس‌پذیری جدید</b>: بدون برنامهٔ پذیرش حامل هیچ تاریخ تحویلی ساخته نمی‌شود.
    /// <c>false</c> فقط برای خوانایی/بازاعتبارسنجی شاهد تاریخی است که خودش تقویم حامل ندارد.
    /// </param>
    public async Task<StorefrontCapturedGroupProjectionResult> ComposeFromFactsAsync(
        SubmitCheckoutCommand command,
        IReadOnlyList<StorefrontCapturedGroupPlan>? groups,
        CheckoutShippingGroupGoodsFactsResult facts,
        CancellationToken cancellationToken,
        bool requirePlannedDispatch = true)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(facts);
        var plan = ValidatePlan(groups);

        // مرجع سبد/نسخه فقط فرمان مورداعتماد است؛ فکت نمی‌تواند سبد خودش را «تأیید» کند.
        ValidateFacts(facts, command.CartId, command.ExpectedCartVersion, plan);

        // یک خواندن ساعت سرور برای کل ترکیب؛ تبدیل به تاریخ محلی هر روش جدا انجام می‌شود.
        var nowUtc = clock.UtcNow;
        return await ComposeValidatedAsync(plan, facts, nowUtc, cancellationToken, requirePlannedDispatch);
    }

    private async Task<StorefrontCapturedGroupProjectionResult> ComposeValidatedAsync(
        List<StorefrontCapturedGroupPlan> plan,
        CheckoutShippingGroupGoodsFactsResult facts,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken,
        bool requirePlannedDispatch)
    {
        var groups = new List<StorefrontCapturedGroupProjection>(plan.Count);
        for (var index = 0; index < plan.Count; index++)
        {
            var planGroup = plan[index];
            var groupFacts = facts.Groups[index];
            var candidates = new List<StorefrontCapturedCandidateProjection>(planGroup.Candidates.Count);
            foreach (var candidate in planGroup.Candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                candidates.Add(await ComposeCandidateAsync(
                    candidate,
                    groupFacts,
                    nowUtc,
                    requirePlannedDispatch,
                    cancellationToken));
            }

            groups.Add(new StorefrontCapturedGroupProjection(
                groupFacts.GroupKey,
                groupFacts.SellerPartyId,
                groupFacts.Currency,
                groupFacts.FinalGoodsAmountAfterDiscountAndTax,
                groupFacts.QuantityTotal,
                groupFacts.GroupWeightGrams,
                groupFacts.PhysicalItemCount,
                groupFacts.CartLineIds,
                candidates));
        }

        return new StorefrontCapturedGroupProjectionResult(
            facts.CartId,
            facts.CartVersion,
            facts.Currency,
            nowUtc,
            groups);
    }

    private async Task<StorefrontCapturedCandidateProjection> ComposeCandidateAsync(
        StorefrontCapturedCandidatePlan candidate,
        CheckoutShippingGroupGoodsFacts groupFacts,
        DateTimeOffset nowUtc,
        bool requirePlannedDispatch,
        CancellationToken cancellationToken)
    {
        // اندازهٔ فیزیکی همان‌جا که منبع Order می‌گوید ناشناخته است ناشناخته می‌ماند؛ هیچ تبدیل
        // quantity→itemcount، گِرد کردن یا حدس واحدی انجام نمی‌شود.
        var request = new ShippingCapturedQuoteLookupRequest(
            candidate.OwnerKind,
            NormalizeOwnerText(candidate.OwnerTenantId),
            candidate.OwnerSellerPartyId,
            candidate.DraftId,
            candidate.SourceRevision,
            candidate.Destination,
            GroupWeightGrams: groupFacts.GroupWeightGrams,
            GroupItemCount: null,
            FinalGroupGoodsAmountAfterDiscountAndTax: groupFacts.FinalGoodsAmountAfterDiscountAndTax,
            CurrencyCode: groupFacts.Currency);

        var result = await capturedQuoteLookup.QuoteAsync(request, cancellationToken);
        if (result is null || result.IsFailure)
        {
            return Unavailable(candidate, StorefrontCapturedGroupProjectionReasons.QuoteFailed);
        }

        var quote = result.Value;
        if (!quote.IsQuoted)
        {
            // کد دلیل مالک دست‌نخورده عبور می‌کند؛ هیچ صفر/رایگان/نرخ قدیمی جای آن نیست.
            return Unavailable(
                candidate,
                string.IsNullOrWhiteSpace(quote.ReasonCode)
                    ? StorefrontCapturedGroupProjectionReasons.QuoteFailed
                    : quote.ReasonCode,
                ownerResult: quote);
        }

        if (!IsBoundToCandidate(quote, candidate, groupFacts.Currency))
        {
            // پاسخ متعلق به نامزد/بازبینی/ارز دیگری است: به‌عنوان قیمت این نامزد پذیرفته نمی‌شود.
            return Unavailable(candidate, StorefrontCapturedGroupProjectionReasons.BindingMismatch);
        }

        var receiving = BuildReceiving(candidate, quote, nowUtc, requirePlannedDispatch);
        return new StorefrontCapturedCandidateProjection(
            candidate,
            StorefrontCapturedCandidateOutcome.Quoted,
            quote.ReasonCode,
            quote,
            receiving);
    }

    /// <summary>
    /// تقویم دریافت همین نامزد را از تقویم <b>مستقل پذیرش حامل</b> (در صورت وجود) و MAX روزهای
    /// ذخیره‌شده می‌سازد. هیچ آماده‌سازی فروشنده دوباره اضافه نمی‌شود و هیچ بازهٔ ثابتی ساخته نمی‌شود.
    /// </summary>
    private static StorefrontCapturedReceivingDates? BuildReceiving(
        StorefrontCapturedCandidatePlan candidate,
        ShippingCapturedQuoteResult quote,
        DateTimeOffset nowUtc,
        bool requirePlannedDispatch)
    {
        if (quote.ReceivingWeek is null)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.ReceivingWeekMissing);
        }

        if (candidate.AvailableDateCount < 0
            || candidate.AvailableDateCount > StorefrontShippingCalculator.MaxReceivingHorizonDays)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.AvailableDateCountInvalid);
        }

        if (quote.MaxDeliveryDays is not int maximumDays
            || maximumDays < 0
            || maximumDays > StorefrontShippingCalculator.MaxReceivingWorkingDayOffset)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.MaximumDeliveryDaysInvalid);
        }

        if (quote.MinDeliveryDays is not int minimumDays
            || minimumDays < 0
            || minimumDays > StorefrontShippingCalculator.MaxReceivingWorkingDayOffset)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.MinimumDeliveryDaysInvalid);
        }

        // TB-256: MAX is the commitment. A stored pair where MAX < MIN is incoherent evidence, never
        // silently clamped down to MIN.
        if (maximumDays < minimumDays)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.MaximumDeliveryDaysInvalid);
        }

        // TB-256 / TB-256-R1: a NEW availability commitment requires the independent carrier acceptance
        // calendar. Without it there is no defensible handoff, so no delivery date is invented from the
        // customer week. Compatibility (historical accepted evidence with a null plan) is an explicit opt-in.
        if (quote.CarrierWeek is not { } carrierWeek)
        {
            return requirePlannedDispatch
                ? NoDates(StorefrontCapturedGroupProjectionReasons.CarrierScheduleMissing)
                : BuildLegacyReceiving(candidate, quote, maximumDays, nowUtc);
        }

        // TB-256: the independent carrier acceptance calendar is present, so the SAME pure authority
        // resolves the handoff cutoff, counts MAX transport working days and filters customer days.
        return BuildPlannedReceiving(candidate, quote, carrierWeek, maximumDays, nowUtc);
    }

    /// <summary>
    /// مسیر سازگاری برای شاهد <b>تاریخی</b> بدون تقویم حامل: همان مکانیک ۲۱۳ (بدون cutoff/موعد حدسی)
    /// اما با قاعدهٔ تعهد TB-256 (MAX نه MIN). این مسیر هرگز برای تعهد جدید استفاده نمی‌شود.
    /// </summary>
    private static StorefrontCapturedReceivingDates? BuildLegacyReceiving(
        StorefrontCapturedCandidatePlan candidate,
        ShippingCapturedQuoteResult quote,
        int maximumDays,
        DateTimeOffset nowUtc)
    {
        if (!TryBuildWeek(quote.ReceivingWeek!, out var week))
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.ReceivingWeekInvalid);
        }

        if (!TryResolveTimeZone(quote.ReceivingWeek!.TimeZoneId, out var timeZone))
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.ReceivingTimeZoneInvalid);
        }

        var referenceLocalDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(nowUtc, timeZone).DateTime);

        StorefrontShippingReceivingCalendar calendar;
        try
        {
            // TB-256: offset = MAX days. No double preparation and no fixed window.
            calendar = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
                referenceLocalDate,
                maximumDays,
                week,
                candidate.AvailableDateCount,
                week.SlotDurationMinutes);
        }
        catch (StorefrontOrderException)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.ReceivingWeekInvalid);
        }

        if (calendar.EarliestEligibleDate is null)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.NoWorkingDays);
        }

        return new StorefrontCapturedReceivingDates(
            calendar.EarliestEligibleDate,
            calendar.AvailableDates,
            UnavailableReason: null);
    }

    /// <summary>
    /// TB-256: تقویم مشتری با مرجع <b>یکتای</b> زمان‌بندی — اولین پذیرش ممکن حامل، شمارش MAX روز کاری
    /// حمل روی روزهای کاری <b>حامل</b> و سپس فیلتر تاریخ/نوبت‌های مشتری. خروجی همان فهرست مسطح
    /// نوبت‌های ۲۱۳ است تا مصرف‌کننده‌های موجود دست‌نخورده بمانند.
    /// </summary>
    private static StorefrontCapturedReceivingDates BuildPlannedReceiving(
        StorefrontCapturedCandidatePlan candidate,
        ShippingCapturedQuoteResult quote,
        ShippingCapturedCarrierWeek carrierWeek,
        int maximumDays,
        DateTimeOffset nowUtc)
    {
        // TB-264: an explicit physical timing policy is counted ONCE before the first possible carrier
        // acceptance. A method without one keeps the historical reference instant (policy v1).
        var preparationMinutes = quote.Settings?.PreparationMinutes;
        var handoffMinutes = quote.Settings?.HandoffMinutes;

        // TB-264-R2: the customer calendar consumes the SAME processing semantics the capture recorded.
        // Omitting the captured processing week made every WorkingWeek method answer «timing policy
        // unconfigured» on the storefront (mapped to a misleading carrier-schedule reason) even though the
        // operator had saved and published a real warehouse week. The authority remains the single
        // decider; this only supplies the stored evidence to it, exactly like the selection writer does.
        var processingMode = quote.Settings?.ProcessingMode
                             ?? ShippingAuthoringPreparationProcessingMode.Unspecified;
        var processingWeek = processingMode == ShippingAuthoringPreparationProcessingMode.WorkingWeek
            && quote.ProcessingWeek is { } storedProcessingWeek
                ? StorefrontShippingSchedulingMapping.ToSchedulingWeek(storedProcessingWeek)
                : null;

        var plan = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            nowUtc,
            StorefrontShippingSchedulingMapping.ToSchedulingWeek(carrierWeek),
            StorefrontShippingSchedulingMapping.ToSchedulingWeek(quote.ReceivingWeek!),
            maximumDays,
            candidate.AvailableDateCount,
            quote.ReceivingWeek!.SlotDurationMinutes,
            preparationMinutes,
            handoffMinutes,
            processingWeek,
            processingContinuous: processingMode == ShippingAuthoringPreparationProcessingMode.Continuous);

        if (!plan.IsSuccess)
        {
            // Failed scheduling carries its own stable reason; no approximated calendar is produced.
            // The one authority's codes are translated into this projection's own stable vocabulary so the
            // storefront keeps a single, actionable blocker per cause instead of a raw downstream code.
            return NoDates(MapSchedulingReason(plan.ReasonCode));
        }

        var slots = new List<StorefrontShippingReceivingSlot>();
        foreach (var date in plan.Value!.Dates)
        {
            foreach (var slot in date.Slots)
            {
                slots.Add(new StorefrontShippingReceivingSlot(slot.Date, slot.OpensAt, slot.ClosesAt));
            }
        }

        return new StorefrontCapturedReceivingDates(
            plan.Value.EarliestEligibleDate,
            slots,
            UnavailableReason: null);
    }

    private static StorefrontCapturedReceivingDates NoDates(string reason) =>
        new(null, Array.Empty<StorefrontShippingReceivingSlot>(), reason);

    /// <summary>
    /// TB-256: کدهای پایدار «یک مرجع زمان‌بندی» را به واژگان پایدار همین projection نگاشت می‌کند تا
    /// فروشگاه برای هر علت دقیقاً یک مانع قابل‌اقدام داشته باشد (بدون کد خام ماژول دیگر).
    /// </summary>
    private static string MapSchedulingReason(string reasonCode) => reasonCode switch
    {
        ShippingDispatchSchedulingReasons.CustomerWeekInvalid
            => StorefrontCapturedGroupProjectionReasons.ReceivingWeekInvalid,
        ShippingDispatchSchedulingReasons.CustomerTimeZoneUnresolvable
            => StorefrontCapturedGroupProjectionReasons.ReceivingTimeZoneInvalid,
        ShippingDispatchSchedulingReasons.NoCustomerWorkingDay
            => StorefrontCapturedGroupProjectionReasons.NoWorkingDays,
        ShippingDispatchSchedulingReasons.TransportDaysInvalid
            => StorefrontCapturedGroupProjectionReasons.MaximumDeliveryDaysInvalid,
        ShippingDispatchSchedulingReasons.HorizonInvalid
            => StorefrontCapturedGroupProjectionReasons.AvailableDateCountInvalid,
        // TB-264-R2: preparation/processing evidence is NOT the carrier's calendar. A method whose
        // timing policy is absent or unusable must say so, never blame the carrier acceptance week.
        ShippingDispatchSchedulingReasons.TimingPolicyUnconfigured
            or ShippingDispatchSchedulingReasons.TimingPolicyIncomplete
            => StorefrontCapturedGroupProjectionReasons.ProcessingScheduleMissing,
        ShippingDispatchSchedulingReasons.ProcessingWeekInvalid
            or ShippingDispatchSchedulingReasons.NoProcessingWorkingDay
            or ShippingDispatchSchedulingReasons.ProcessingTimeZoneUnresolvable
            or ShippingDispatchSchedulingReasons.PreparationMinutesInvalid
            or ShippingDispatchSchedulingReasons.HandoffMinutesInvalid
            or ShippingDispatchSchedulingReasons.PreparationSearchExhausted
            => StorefrontCapturedGroupProjectionReasons.ProcessingScheduleInvalid,
        // Every remaining failure belongs to the carrier acceptance calendar itself (invalid/duplicate
        // week, unresolvable zone, no working day, unusable slot or no feasible handoff).
        _ => StorefrontCapturedGroupProjectionReasons.CarrierScheduleInvalid,
    };

    /// <summary>
    /// هفتهٔ ذخیره‌شده را به مدل محاسبهٔ 213 نگاشت می‌کند؛ ISO ۱..۷ => دوشنبه..یکشنبه. روز تعطیل بازه
    /// نمی‌سازد (نگاشت داخلی‌اش هرگز به‌عنوان ساعت ذخیره‌شده گزارش نمی‌شود) و روز کاری بازهٔ مثبت لازم دارد.
    /// </summary>
    private static bool TryBuildWeek(
        ShippingCapturedReceivingWeek stored,
        out StorefrontShippingReceivingWeek week)
    {
        week = null!;
        if (stored.Days is null || stored.Days.Count != 7)
        {
            return false;
        }

        var days = new List<StorefrontShippingReceivingDay>(7);
        var seen = new HashSet<int>();
        foreach (var day in stored.Days)
        {
            if (day is null || day.Weekday is < 1 or > 7 || !seen.Add(day.Weekday))
            {
                return false;
            }

            if (day.IsWorking)
            {
                if (day.StartTime is not TimeOnly opens
                    || day.EndTime is not TimeOnly closes
                    || closes <= opens)
                {
                    return false;
                }

                days.Add(new StorefrontShippingReceivingDay(IsoWeekday(day.Weekday), true, opens, closes));
                continue;
            }

            days.Add(new StorefrontShippingReceivingDay(
                IsoWeekday(day.Weekday),
                false,
                TimeOnly.MinValue,
                TimeOnly.MinValue));
        }

        week = new StorefrontShippingReceivingWeek(days, stored.SlotDurationMinutes);
        return true;
    }

    /// <summary>ISO weekday (۱..۷) به <see cref="DayOfWeek"/> با دوشنبه به‌عنوان ۱.</summary>
    private static DayOfWeek IsoWeekday(int isoWeekday) => (DayOfWeek)(isoWeekday % 7);

    private static bool TryResolveTimeZone(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;
        var id = timeZoneId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    /// <summary>
    /// تعهد پاسخ: سرویس/گزینهٔ موردانتظار، همان پیش‌نویس و همان بازبینی، ارز گروه و مبالغ کامل غیرمنفی.
    /// </summary>
    private static bool IsBoundToCandidate(
        ShippingCapturedQuoteResult quote,
        StorefrontCapturedCandidatePlan candidate,
        string currency)
    {
        if (!string.Equals(
                NormalizeOwnerText(quote.ServiceCode),
                NormalizeOwnerText(candidate.ServiceCode),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(
                NormalizeOwnerText(quote.OptionCode) ?? string.Empty,
                NormalizeOwnerText(candidate.OptionCode) ?? string.Empty,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (quote.DraftId != candidate.DraftId
            || quote.SourceRevision != candidate.SourceRevision
            || !string.Equals(
                NormalizeOwnerText(quote.CurrencyCode),
                NormalizeOwnerText(currency),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return quote.FreightAmount is >= 0m
            && quote.PackagingAmount is >= 0m
            && quote.TotalExpenseAmount is >= 0m
            && quote.PayableAtCheckoutAmount is >= 0m
            && quote.CarrierCollectAmount is >= 0m;
    }

    private static StorefrontCapturedCandidateProjection Unavailable(
        StorefrontCapturedCandidatePlan candidate,
        string reasonCode,
        ShippingCapturedQuoteResult? ownerResult = null) =>
        new(candidate, StorefrontCapturedCandidateOutcome.Unavailable, reasonCode, ownerResult, Receiving: null);

    private static string? NormalizeOwnerText(string? value) =>
        StorefrontCapturedPublicationBinding.Canonical(value);

    /// <summary>
    /// شبکهٔ گروه/نامزد را اعتبارسنجی می‌کند: بدون کلید تکراری/خالی، بدون خط تکراری، بدون دو نامزد
    /// هم‌هویت یا دو بازبینی یک تعهد. هیچ فهرستی بی‌صدا کوچک نمی‌شود.
    /// </summary>
    private static List<StorefrontCapturedGroupPlan> ValidatePlan(
        IReadOnlyList<StorefrontCapturedGroupPlan>? groups)
    {
        if (groups is null || groups.Count == 0)
        {
            throw PlanInvalid();
        }

        var seenGroupKeys = new HashSet<string>(StringComparer.Ordinal);
        var seenCartLineIds = new HashSet<Guid>();
        // یک بازبینی انتخاب‌شده برای هر تعهد در کل درخواست: مالک + سرویس/گزینه، بدون مقصد گروه.
        var selectedBinding = new Dictionary<StorefrontCapturedPublicationBinding, (Guid DraftId, long Revision)>();
        var validated = new List<StorefrontCapturedGroupPlan>(groups.Count);

        foreach (var group in groups)
        {
            if (group is null || string.IsNullOrWhiteSpace(group.GroupKey))
            {
                throw PlanInvalid();
            }

            if (group.GroupKey.Length != group.GroupKey.Trim().Length || !seenGroupKeys.Add(group.GroupKey))
            {
                throw PlanInvalid();
            }

            if (group.CartLineIds is null || group.CartLineIds.Count == 0)
            {
                throw PlanInvalid();
            }

            foreach (var cartLineId in group.CartLineIds)
            {
                if (cartLineId == Guid.Empty || !seenCartLineIds.Add(cartLineId))
                {
                    throw PlanInvalid();
                }
            }

            if (group.Candidates is null)
            {
                throw PlanInvalid();
            }

            ValidateCandidates(group.Candidates, selectedBinding);
            validated.Add(group);
        }

        return validated;
    }

    private static void ValidateCandidates(
        IReadOnlyList<StorefrontCapturedCandidatePlan> candidates,
        Dictionary<StorefrontCapturedPublicationBinding, (Guid DraftId, long Revision)> selectedBinding)
    {
        var seenMethodCodes = new HashSet<string>(StringComparer.Ordinal);
        var seenIdentities = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (candidate is null
                || string.IsNullOrWhiteSpace(candidate.MethodCode)
                || candidate.MethodCode.Length != candidate.MethodCode.Trim().Length
                || !seenMethodCodes.Add(candidate.MethodCode)
                || string.IsNullOrWhiteSpace(candidate.ServiceCode)
                || candidate.ServiceCode.Length != candidate.ServiceCode.Trim().Length
                || candidate.Destination is null
                || candidate.Destination.CountryId == Guid.Empty
                || candidate.DraftId == Guid.Empty
                || candidate.SourceRevision <= 0
                || candidate.AvailableDateCount < 0
                || candidate.AvailableDateCount > StorefrontShippingCalculator.MaxReceivingHorizonDays)
            {
                throw PlanInvalid();
            }

            if (!Enum.IsDefined(candidate.OwnerKind) || !OwnerKeyCoherent(candidate))
            {
                throw PlanInvalid();
            }

            var identity = string.Join(
                '\u001f',
                candidate.MethodCode,
                (int)candidate.OwnerKind,
                NormalizeOwnerText(candidate.OwnerTenantId) ?? string.Empty,
                candidate.OwnerSellerPartyId?.ToString("N") ?? string.Empty,
                candidate.DraftId.ToString("N"),
                candidate.SourceRevision,
                candidate.Destination.CountryId.ToString("N"),
                candidate.Destination.AdministrativeAreaId?.ToString("N") ?? string.Empty,
                candidate.Destination.CityId?.ToString("N") ?? string.Empty,
                candidate.Destination.ServiceAreaId?.ToString("N") ?? string.Empty,
                candidate.ServiceCode,
                NormalizeOwnerText(candidate.OptionCode) ?? string.Empty);

            if (!seenIdentities.Add(identity))
            {
                throw PlanInvalid();
            }

            // همان تعهد با همان پیش‌نویس و همان بازبینی در گروه دیگر مجاز و لازم است؛ فقط تعارض
            // پیش‌نویس/بازبینی رد می‌شود. مقصد و برچسب روش هویت انتشار نیستند.
            var binding = new StorefrontCapturedPublicationBinding(candidate);
            if (selectedBinding.TryGetValue(binding, out var existing))
            {
                if (existing.DraftId != candidate.DraftId || existing.Revision != candidate.SourceRevision)
                {
                    throw PlanInvalid();
                }

                continue;
            }

            selectedBinding[binding] = (candidate.DraftId, candidate.SourceRevision);
        }
    }

    private static bool OwnerKeyCoherent(StorefrontCapturedCandidatePlan candidate) =>
        candidate.OwnerKind switch
        {
            ShippingCapturedQuoteOwnerKind.Store =>
                !string.IsNullOrWhiteSpace(candidate.OwnerTenantId) && candidate.OwnerSellerPartyId is null,
            ShippingCapturedQuoteOwnerKind.Marketplace =>
                string.IsNullOrWhiteSpace(candidate.OwnerTenantId) && candidate.OwnerSellerPartyId is null,
            ShippingCapturedQuoteOwnerKind.Seller =>
                string.IsNullOrWhiteSpace(candidate.OwnerTenantId)
                && candidate.OwnerSellerPartyId is Guid sellerId
                && sellerId != Guid.Empty,
            _ => false,
        };

    /// <summary>
    /// فکت‌های مالی باید همان سبد/نسخه/گروه‌ها/عضوهای درخواستی باشند؛ گروه اضافی/غایب یا عضو جابه‌جا
    /// پذیرفته نمی‌شود.
    /// </summary>
    private static void ValidateFacts(
        CheckoutShippingGroupGoodsFactsResult? facts,
        Guid cartId,
        int cartVersion,
        List<StorefrontCapturedGroupPlan> plan)
    {
        if (facts is null
            || facts.Groups.Count != plan.Count
            || facts.CartId != cartId
            || facts.CartVersion != cartVersion
            || string.IsNullOrWhiteSpace(facts.Currency))
        {
            throw FactsMismatch();
        }

        for (var index = 0; index < plan.Count; index++)
        {
            var groupFacts = facts.Groups[index];
            if (!string.Equals(groupFacts.GroupKey, plan[index].GroupKey, StringComparison.Ordinal)
                || !string.Equals(groupFacts.Currency, facts.Currency, StringComparison.Ordinal)
                || groupFacts.CartLineIds.Count != plan[index].CartLineIds.Count
                || !groupFacts.CartLineIds.SequenceEqual(plan[index].CartLineIds)
                || groupFacts.FinalGoodsAmountAfterDiscountAndTax < 0m)
            {
                throw FactsMismatch();
            }
        }
    }

    private static ContractOperationException PlanInvalid() =>
        new(StorefrontCapturedGroupProjectionReasons.PlanInvalid);

    private static ContractOperationException FactsMismatch() =>
        new(StorefrontCapturedGroupProjectionReasons.FactsMismatch);
}
