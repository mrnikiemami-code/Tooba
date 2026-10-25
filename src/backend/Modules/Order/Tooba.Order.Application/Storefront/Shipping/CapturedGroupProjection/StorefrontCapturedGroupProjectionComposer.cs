using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;

/// <summary>
/// تنها نقطهٔ ترکیب سه منبع پذیرفته‌شده برای یک گروه ارسال: فکت واقعی مبلغ کالای همان گروه پس از
/// تخفیف و مالیات (منبع Order)، قیمت یک بازبینی capture‌شدهٔ دقیق (Contracts مالک Shipping) و تقویم
/// دریافت مشتری از هفتهٔ ذخیره‌شده همان روش.
/// </summary>
/// <remarks>
/// <para>
/// <b>Compose, don't recalculate.</b> نه مالیات/تخفیف دوباره حساب می‌شود، نه مبلغ از ورودی فراخوان،
/// نه تقویم از پنجرهٔ ثابت یا ساعت مرورگر. مبلغ گروه یک‌بار از منبع Order و قیمت هر نامزد یک‌بار از
/// مالک Shipping خوانده می‌شود.
/// </para>
/// <para>
/// <b>No activation, no owner decision.</b> این سرویس هیچ بازبینی منتشرشده/فعال را انتخاب نمی‌کند،
/// هیچ مالک fulfillment را از فروشنده استنتاج نمی‌کند و هیچ مسیر عمومی/HTTP نمی‌سازد. برنامهٔ نامزدها
/// باید از یک انتخاب سرور بازبینی‌شده بیاید، هرگز از مرورگر.
/// </para>
/// <para>
/// <b>Read-only.</b> هیچ Checkout/Order/رزرو/تبدیل سبد/پرداختی نوشته نمی‌شود.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedGroupProjectionComposer(
    ICheckoutShippingGroupGoodsFactsReader goodsFactsReader,
    IShippingCapturedRevisionQuoteLookup capturedQuoteLookup,
    IClock clock)
{
    /// <summary>
    /// برنامهٔ گروه‌ها و نامزدها را اعتبارسنجی می‌کند، فکت‌های مالی را یک‌بار برای کل سبد می‌خواند و
    /// برای هر نامزد یک قیمت دقیق و یک تقویم دریافت می‌سازد.
    /// </summary>
    /// <param name="request">درخواست با فرمان checkout و برنامهٔ گروه/نامزد.</param>
    /// <param name="cancellationToken">توکن لغو؛ بدون تغییر به درزهای بیرونی می‌رسد.</param>
    public async Task<StorefrontCapturedGroupProjectionResult> ComposeAsync(
        StorefrontCapturedGroupProjectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = request.Command ?? throw PlanInvalid();
        var plan = ValidatePlan(request.Groups);

        // یک‌بار برای کل سبد، با همان دسترسی/نسخهٔ مورداعتماد؛ هرگز پیش‌نمایش جدا برای هر گروه.
        var facts = await goodsFactsReader.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                command,
                plan.Select(group => new CheckoutShippingGroupMembership(group.GroupKey, group.CartLineIds))
                    .ToArray()),
            cancellationToken);

        ValidateFacts(facts, command, plan);

        // یک خواندن ساعت سرور برای کل ترکیب؛ تبدیل به تاریخ محلی هر روش جدا انجام می‌شود.
        var nowUtc = clock.UtcNow;

        var groups = new List<StorefrontCapturedGroupProjection>(plan.Count);
        for (var index = 0; index < plan.Count; index++)
        {
            var planGroup = plan[index];
            var groupFacts = facts.Groups[index];
            var candidates = new List<StorefrontCapturedCandidateProjection>(planGroup.Candidates.Count);
            foreach (var candidate in planGroup.Candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                candidates.Add(await ComposeCandidateAsync(candidate, groupFacts, nowUtc, cancellationToken));
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

        var receiving = BuildReceiving(candidate, quote, nowUtc);
        return new StorefrontCapturedCandidateProjection(
            candidate,
            StorefrontCapturedCandidateOutcome.Quoted,
            quote.ReasonCode,
            quote,
            receiving);
    }

    /// <summary>
    /// تقویم دریافت همین نامزد را از هفتهٔ <b>ذخیره‌شدهٔ همان روش</b> و کمینهٔ روزهای ذخیره‌شده می‌سازد.
    /// هیچ آماده‌سازی فروشنده دوباره اضافه نمی‌شود و هیچ بازهٔ ثابتی ساخته نمی‌شود.
    /// </summary>
    private static StorefrontCapturedReceivingDates? BuildReceiving(
        StorefrontCapturedCandidatePlan candidate,
        ShippingCapturedQuoteResult quote,
        DateTimeOffset nowUtc)
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

        if (quote.MinDeliveryDays is not int minimumDays
            || minimumDays < 0
            || minimumDays > StorefrontShippingCalculator.MaxReceivingWorkingDayOffset)
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.MinimumDeliveryDaysInvalid);
        }

        if (!TryBuildWeek(quote.ReceivingWeek, out var week))
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.ReceivingWeekInvalid);
        }

        if (!TryResolveTimeZone(quote.ReceivingWeek.TimeZoneId, out var timeZone))
        {
            return NoDates(StorefrontCapturedGroupProjectionReasons.ReceivingTimeZoneInvalid);
        }

        var referenceLocalDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(nowUtc, timeZone).DateTime);

        StorefrontShippingReceivingCalendar calendar;
        try
        {
            // همین محاسبهٔ پذیرفته‌شدهٔ 213: offset = کمینهٔ روزهای ذخیره‌شده، بدون افزایش آماده‌سازی.
            calendar = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
                referenceLocalDate,
                minimumDays,
                week,
                candidate.AvailableDateCount);
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

    private static StorefrontCapturedReceivingDates NoDates(string reason) =>
        new(null, Array.Empty<StorefrontShippingReceivingSlot>(), reason);

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

        week = new StorefrontShippingReceivingWeek(days);
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
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

            ValidateCandidates(group.Candidates);
            validated.Add(group);
        }

        return validated;
    }

    private static void ValidateCandidates(IReadOnlyList<StorefrontCapturedCandidatePlan> candidates)
    {
        var seenMethodCodes = new HashSet<string>(StringComparer.Ordinal);
        var seenIdentities = new HashSet<string>(StringComparer.Ordinal);
        var seenBindings = new Dictionary<string, (Guid DraftId, long Revision)>(StringComparer.Ordinal);

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

            // یک تعهد (مالک + مقصد + سرویس/گزینه) نباید هم‌زمان دو بازبینی/پیش‌نویس داشته باشد.
            var binding = string.Join(
                '\u001f',
                (int)candidate.OwnerKind,
                NormalizeOwnerText(candidate.OwnerTenantId) ?? string.Empty,
                candidate.OwnerSellerPartyId?.ToString("N") ?? string.Empty,
                candidate.Destination.CountryId.ToString("N"),
                candidate.Destination.AdministrativeAreaId?.ToString("N") ?? string.Empty,
                candidate.Destination.CityId?.ToString("N") ?? string.Empty,
                candidate.Destination.ServiceAreaId?.ToString("N") ?? string.Empty,
                candidate.ServiceCode,
                NormalizeOwnerText(candidate.OptionCode) ?? string.Empty);

            if (seenBindings.TryGetValue(binding, out var existing)
                && (existing.DraftId != candidate.DraftId || existing.Revision != candidate.SourceRevision))
            {
                throw PlanInvalid();
            }

            seenBindings[binding] = (candidate.DraftId, candidate.SourceRevision);
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
        SubmitCheckoutCommand command,
        List<StorefrontCapturedGroupPlan> plan)
    {
        if (facts is null
            || facts.Groups.Count != plan.Count
            || facts.CartId != command.CartId
            || facts.CartVersion != command.ExpectedCartVersion
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
