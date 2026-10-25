using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-218: ترکیب سه منبع واقعی — فکت مبلغ گروه پس از تخفیف و مالیات (Order)، قیمت یک بازبینی
/// capture‌شدهٔ دقیق (Contracts مالک Shipping) و تقویم دریافت 213 — بدون بازمحاسبه و بدون فعال‌سازی.
/// </summary>
[Collection("ShippingGroupGoodsFacts")]
public sealed class StorefrontCapturedGroupProjectionComposerTests
{
    private static readonly Guid LineA = Guid.Parse("0199f111-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("0199f111-0000-7000-8000-00000000000b");
    private static readonly Guid OfferA = Guid.Parse("0199f222-0000-7000-8000-00000000000a");
    private static readonly Guid OfferB = Guid.Parse("0199f222-0000-7000-8000-00000000000b");
    private static readonly Guid DraftA = Guid.Parse("0199f333-0000-7000-8000-00000000000a");
    private static readonly Guid DraftB = Guid.Parse("0199f333-0000-7000-8000-00000000000b");
    private static readonly Guid CountryId = Guid.Parse("0199f444-0000-7000-8000-000000000001");
    private static readonly Guid CityId = Guid.Parse("0199f444-0000-7000-8000-000000000002");
    private static readonly Guid RowA = Guid.Parse("0199f555-0000-7000-8000-00000000000a");
    private static readonly Guid RowB = Guid.Parse("0199f555-0000-7000-8000-00000000000b");

    /// <summary>
    /// دو گروه از یک فروشنده: 100−10+9 = 99 و 200−20+18 = 198 از مسیر واقعی Promotion→Tax؛ همان
    /// مقادیر به مالک Shipping می‌رسند و هر گروه مؤلفه/provenance جدای خودش را نگه می‌دارد.
    /// </summary>
    [Fact]
    public async Task Two_groups_compose_real_99_and_198_goods_amounts_with_separate_quote_provenance()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m),
            harness.Line(LineB, OfferB, 200m));
        harness.Promotions.Discounts[OfferA] = 10m;
        harness.Promotions.Discounts[OfferB] = 20m;
        var shipping = new RecordingCapturedQuoteLookup();
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero));
        var composer = new StorefrontCapturedGroupProjectionComposer(harness.Directory, shipping, clock);
        var week = Week("Asia/Tehran", (6, 9, 0, 12, 0), (7, 10, 15, 13, 45), (2, 8, 30, 11, 0));

        shipping.Respond = request => request.DraftId == DraftA
            ? Quoted(request, freight: 15_000m, packaging: 2_000m, groupId: RowA, rateRowId: RowA, week: week)
            : Quoted(request, freight: 25_000m, packaging: 0m, serviceCode: "standard", optionCode: "std", groupId: RowB, rateRowId: RowB, week: week);

        var result = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 3, methodCode: "express", availableDateCount: 3)),
                    Plan("g2", [LineB], Candidate(DraftB, revision: 5, methodCode: "standard", serviceCode: "standard")),
                ]),
            CancellationToken.None);

        Assert.Equal(harness.CartId, result.CartId);
        Assert.Equal(7, result.CartVersion);
        Assert.Equal(ShippingGroupGoodsFactsHarness.Currency, result.Currency);
        Assert.Equal(clock.Value, result.EvaluatedAtUtc);
        Assert.Equal(2, result.Groups.Count);

        var first = result.Groups[0];
        Assert.Equal("g1", first.GroupKey);
        Assert.Equal(99m, first.FinalGoodsAmountAfterDiscountAndTax);
        Assert.Null(first.GroupWeightGrams);
        Assert.Null(first.PhysicalItemCount);
        var firstQuote = Assert.Single(first.Candidates);
        Assert.Equal(StorefrontCapturedCandidateOutcome.Quoted, firstQuote.Outcome);
        Assert.Equal(15_000m, firstQuote.OwnerResult!.FreightAmount);
        Assert.Equal(2_000m, firstQuote.OwnerResult.PackagingAmount);
        Assert.Equal(17_000m, firstQuote.OwnerResult.TotalExpenseAmount);
        Assert.Equal(17_000m, firstQuote.OwnerResult.PayableAtCheckoutAmount);
        Assert.Equal(0m, firstQuote.OwnerResult.CarrierCollectAmount);
        Assert.Equal(ShippingCapturedQuoteCustomerEligibility.NotEvaluated, firstQuote.OwnerResult.Eligibility);
        Assert.Equal(RowA, firstQuote.OwnerResult.RateRowId);

        var second = result.Groups[1];
        Assert.Equal(198m, second.FinalGoodsAmountAfterDiscountAndTax);
        var secondQuote = Assert.Single(second.Candidates);
        Assert.Equal(25_000m, secondQuote.OwnerResult!.FreightAmount);
        Assert.Equal(0m, secondQuote.OwnerResult.PackagingAmount);
        Assert.Equal(RowB, secondQuote.OwnerResult.RateRowId);
        Assert.NotEqual(firstQuote.OwnerResult.SnapshotId, secondQuote.OwnerResult.SnapshotId);

        // اندازهٔ مالی هر نامزد همان مبلغ واقعی گروه خودش بود؛ هیچ‌کدام جمع کل سبد (297) نیست.
        Assert.Equal(2, shipping.Requests.Count);
        Assert.Equal(99m, shipping.Requests[0].FinalGroupGoodsAmountAfterDiscountAndTax);
        Assert.Equal(198m, shipping.Requests[1].FinalGroupGoodsAmountAfterDiscountAndTax);
        Assert.Equal(ShippingGroupGoodsFactsHarness.Currency, shipping.Requests[0].CurrencyCode);
        Assert.Null(shipping.Requests[0].GroupWeightGrams);
        Assert.Null(shipping.Requests[0].GroupItemCount);
        Assert.Equal(3, shipping.Requests[0].SourceRevision);
        Assert.Equal(DraftA, shipping.Requests[0].DraftId);

        // یک نقل‌قول whole-cart: هر خط یک‌بار Pricing/Promotion/Tax، سبد یک‌بار خوانده شد.
        Assert.Equal(1, harness.Carts.Calls);
        Assert.Equal(2, harness.Prices.Calls);
        Assert.Equal(2, harness.Promotions.Calls);
        Assert.Equal(2, harness.Taxes.Calls);
        Assert.Equal(1, clock.Reads);

        // تقویم از هفتهٔ همان روش و کمینهٔ ذخیره‌شده (بدون آماده‌سازی فروشنده) ساخته شد.
        Assert.Equal(new DateOnly(2026, 10, 27), firstQuote.Receiving!.EarliestEligibleDate);
        Assert.Equal(3, firstQuote.Receiving.AvailableDates.Count);
        Assert.Equal(new TimeOnly(8, 30), firstQuote.Receiving.AvailableDates[0].OpensAt);
        Assert.Equal(new TimeOnly(11, 0), firstQuote.Receiving.AvailableDates[0].ClosesAt);
    }

    /// <summary>
    /// کمینه ۲ با شنبه/یکشنبه کاری، دوشنبه تعطیل: زودترین تاریخ سه‌شنبه است و هر روز ساعت خودش را دارد.
    /// </summary>
    [Fact]
    public async Task Minimum_two_working_days_skips_closed_monday_and_keeps_each_days_own_hours()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var shipping = new RecordingCapturedQuoteLookup();
        // 2026-10-24 شنبه است؛ در تهران 11:30 همان روز.
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero));
        var composer = new StorefrontCapturedGroupProjectionComposer(harness.Directory, shipping, clock);
        var week = Week(
            "Asia/Tehran",
            (6, 9, 0, 12, 0),
            (7, 10, 15, 13, 45),
            (2, 8, 30, 11, 0));
        shipping.Respond = request => Quoted(request, 10_000m, 0m, week: week, minDays: 2, maxDays: 4);

        var result = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [Plan("g1", [LineA], Candidate(DraftA, revision: 1, availableDateCount: 3))]),
            CancellationToken.None);

        var candidate = Assert.Single(Assert.Single(result.Groups).Candidates);
        var receiving = candidate.Receiving!;
        Assert.Equal(new DateOnly(2026, 10, 27), receiving.EarliestEligibleDate);
        Assert.Null(receiving.UnavailableReason);
        Assert.Equal(
            [
                new DateOnly(2026, 10, 27),
                new DateOnly(2026, 10, 31),
                new DateOnly(2026, 11, 1),
            ],
            receiving.AvailableDates.Select(slot => slot.Date).ToArray());

        // ساعت هر روز همان ذخیرهٔ همان روز است؛ هیچ بازهٔ ثابت ۹ تا ۱۲ ساخته نمی‌شود.
        Assert.Equal(new TimeOnly(8, 30), receiving.AvailableDates[0].OpensAt);
        Assert.Equal(new TimeOnly(9, 0), receiving.AvailableDates[1].OpensAt);
        Assert.Equal(new TimeOnly(10, 15), receiving.AvailableDates[2].OpensAt);
        Assert.Equal(new TimeOnly(13, 45), receiving.AvailableDates[2].ClosesAt);

        // حداکثر مدت فقط متادیتا می‌ماند.
        Assert.Equal(4, candidate.OwnerResult!.MaxDeliveryDays);
        Assert.Equal(2, candidate.OwnerResult.MinDeliveryDays);
    }

    /// <summary>
    /// یک لحظهٔ UTC با منطقهٔ زمانی هر گروه به تاریخ محلی متفاوت تبدیل می‌شود و کمینهٔ هر گروه
    /// مستقل است (بدون max/آماده‌سازی مشترک).
    /// </summary>
    [Fact]
    public async Task Utc_instant_maps_to_each_groups_own_local_date_and_minimum()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m),
            harness.Line(LineB, OfferB, 200m));
        var shipping = new RecordingCapturedQuoteLookup();
        // 2026-10-24T21:00Z = یکشنبه 00:30 در تهران، ولی هنوز شنبه در UTC.
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 24, 21, 0, 0, TimeSpan.Zero));
        var composer = new StorefrontCapturedGroupProjectionComposer(harness.Directory, shipping, clock);
        var tehran = Week("Asia/Tehran", Enumerable.Range(1, 7).Select(day => (day, 9, 0, 17, 0)).ToArray());
        var utc = Week("UTC", Enumerable.Range(1, 7).Select(day => (day, 9, 0, 17, 0)).ToArray());

        shipping.Respond = request => request.DraftId == DraftA
            ? Quoted(request, 1_000m, 0m, week: tehran, minDays: 1, maxDays: 4)
            : Quoted(request, 2_000m, 0m, serviceCode: "utc", optionCode: "std", week: utc, minDays: 3, maxDays: 7);

        var result = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 1, methodCode: "tehran")),
                    Plan("g2", [LineB], Candidate(DraftB, revision: 1, methodCode: "utc", serviceCode: "utc")),
                ]),
            CancellationToken.None);

        // تهران: یکشنبه 25 → اولین روز کاری بعدی دوشنبه 26.
        Assert.Equal(
            new DateOnly(2026, 10, 26),
            result.Groups[0].Candidates[0].Receiving!.EarliestEligibleDate);
        // UTC: شنبه 24 → سه روز کاری بعد: یکشنبه 25، دوشنبه 26، سه‌شنبه 27.
        Assert.Equal(
            new DateOnly(2026, 10, 27),
            result.Groups[1].Candidates[0].Receiving!.EarliestEligibleDate);

        Assert.Equal(4, result.Groups[0].Candidates[0].OwnerResult!.MaxDeliveryDays);
        Assert.Equal(7, result.Groups[1].Candidates[0].OwnerResult!.MaxDeliveryDays);
    }

    /// <summary>
    /// اندازهٔ ناشناختهٔ نامزد «مبلغ صفر» نمی‌شود: عدم قیمت صریح با کد دلیل مالک برمی‌گردد.
    /// </summary>
    [Fact]
    public async Task Unknown_measurement_is_explicitly_unavailable_and_never_zero()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var shipping = new RecordingCapturedQuoteLookup
        {
            Respond = request => new ShippingCapturedQuoteResult(
                ShippingCapturedQuoteVerdict.MeasurementRequired,
                ShippingCapturedQuoteVerdictCodes.MeasurementRequired,
                ServiceCode: null,
                OptionCode: null,
                CurrencyCode: null,
                FreightAmount: null,
                PackagingAmount: null,
                TotalExpenseAmount: null,
                PayableAtCheckoutAmount: null,
                CarrierCollectAmount: null,
                Settings: null,
                SnapshotId: Guid.Parse("0199f666-0000-7000-8000-000000000001"),
                DraftId: DraftA,
                SourceRevision: 1,
                FormatVersion: 1,
                ShippingTypeId: null,
                GroupId: null,
                RateRowId: null,
                MinDeliveryDays: null,
                MaxDeliveryDays: null,
                ReceivingWeek: null),
        };
        var composer = Create(harness, shipping);

        var result = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [Plan("g1", [LineA], Candidate(DraftA, revision: 1))]),
            CancellationToken.None);

        var candidate = Assert.Single(Assert.Single(result.Groups).Candidates);
        Assert.Equal(StorefrontCapturedCandidateOutcome.Unavailable, candidate.Outcome);
        Assert.Equal(ShippingCapturedQuoteVerdictCodes.MeasurementRequired, candidate.ReasonCode);
        Assert.Null(candidate.Receiving);
        Assert.Null(candidate.OwnerResult!.FreightAmount);
        Assert.Null(candidate.OwnerResult.TotalExpenseAmount);
    }

    /// <summary>رد شدن یک گروه، گروه دیگر را پاک نمی‌کند و فکت‌هایش دست‌نخورده می‌مانند.</summary>
    [Fact]
    public async Task One_groups_refusal_does_not_wipe_another_groups_quote_or_facts()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m),
            harness.Line(LineB, OfferB, 200m));
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);
        var week = Week("UTC", Enumerable.Range(1, 7).Select(day => (day, 9, 0, 17, 0)).ToArray());
        shipping.Respond = request => request.DraftId == DraftA
            ? new ShippingCapturedQuoteResult(
                ShippingCapturedQuoteVerdict.NotConfigured,
                ShippingCapturedQuoteVerdictCodes.NotConfigured,
                null, null, null, null, null, null, null, null, null,
                Guid.Parse("0199f666-0000-7000-8000-000000000002"), DraftA, 1, 1, null, null, null, null, null, null)
            : Quoted(request, 7_000m, 500m, serviceCode: "b", optionCode: "std", week: week);

        var result = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 1, methodCode: "a", serviceCode: "a")),
                    Plan("g2", [LineB], Candidate(DraftB, revision: 1, methodCode: "b", serviceCode: "b")),
                ]),
            CancellationToken.None);

        Assert.Equal(2, result.Groups.Count);
        Assert.Equal(110m, result.Groups[0].FinalGoodsAmountAfterDiscountAndTax);
        Assert.Equal(StorefrontCapturedCandidateOutcome.Unavailable, result.Groups[0].Candidates[0].Outcome);
        Assert.Equal(ShippingCapturedQuoteVerdictCodes.NotConfigured, result.Groups[0].Candidates[0].ReasonCode);
        Assert.Equal(220m, result.Groups[1].FinalGoodsAmountAfterDiscountAndTax);
        Assert.Equal(StorefrontCapturedCandidateOutcome.Quoted, result.Groups[1].Candidates[0].Outcome);
        Assert.Equal(7_500m, result.Groups[1].Candidates[0].OwnerResult!.TotalExpenseAmount);
    }

    /// <summary>
    /// پاسخ متعلق به بازبینی/سرویس/ارز دیگر پذیرفته نمی‌شود و هیچ مبلغی به این نامزد نسبت نمی‌یابد.
    /// </summary>
    [Fact]
    public async Task Quote_bound_to_another_revision_service_or_currency_is_never_adopted()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);

        var wrongRevision = await ComposeSingle(composer, harness, shipping, request =>
            Quoted(request, 1_000m, 0m, sourceRevision: request.SourceRevision + 1));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.BindingMismatch, wrongRevision.ReasonCode);
        Assert.Null(wrongRevision.OwnerResult);

        var wrongService = await ComposeSingle(composer, harness, shipping, request =>
            Quoted(request, 1_000m, 0m, serviceCode: "another-service"));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.BindingMismatch, wrongService.ReasonCode);

        var wrongCurrency = await ComposeSingle(composer, harness, shipping, request =>
            Quoted(request, 1_000m, 0m, currency: "USD"));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.BindingMismatch, wrongCurrency.ReasonCode);

        var wrongDraft = await ComposeSingle(composer, harness, shipping, request =>
            Quoted(request, 1_000m, 0m, draftId: DraftB));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.BindingMismatch, wrongDraft.ReasonCode);

        Assert.Equal(4, shipping.Calls);
        Assert.All(
            new[] { wrongRevision, wrongService, wrongCurrency, wrongDraft },
            candidate => Assert.Equal(StorefrontCapturedCandidateOutcome.Unavailable, candidate.Outcome));
    }

    /// <summary>فکت مالی ساختگی/ناسازگار با درخواست پذیرفته نمی‌شود.</summary>
    [Fact]
    public async Task Fabricated_or_mismatched_goods_facts_fail_closed()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var factsWrongVersion = new CheckoutShippingGroupGoodsFactsResult(
            harness.CartId,
            99,
            ShippingGroupGoodsFactsHarness.Currency,
            []);
        var composer = new StorefrontCapturedGroupProjectionComposer(
            new FixedGoodsFactsReader(factsWrongVersion),
            new RecordingCapturedQuoteLookup(),
            new MutableClock(DateTimeOffset.UtcNow));

        var error = await Assert.ThrowsAsync<ContractOperationException>(() => composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [Plan("g1", [LineA], Candidate(DraftA, revision: 1))]),
            CancellationToken.None));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.FactsMismatch, error.Code);
    }

    /// <summary>
    /// برنامهٔ تکراری/ناقص، دو بازبینی برای یک تعهد، یا کلید مالک ناسازگار پیش از هر فراخوانی رد می‌شود.
    /// </summary>
    [Fact]
    public async Task Malformed_or_duplicate_plan_fails_before_any_foreign_call()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m),
            harness.Line(LineB, OfferB, 200m));
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);

        await AssertPlanInvalid(composer, harness, shipping,
            [
                Plan("g1", [LineA], Candidate(DraftA, revision: 1)),
                Plan("g1", [LineB], Candidate(DraftB, revision: 1)),
            ]);
        await AssertPlanInvalid(composer, harness, shipping,
            [Plan("g1", [LineA, LineB], Candidate(DraftA, revision: 1)), Plan("g1 ", [LineB], Candidate(DraftB, revision: 1))]);
        await AssertPlanInvalid(composer, harness, shipping,
            [
                Plan("g1", [LineA], Candidate(DraftA, revision: 1)),
                Plan("g2", [LineA], Candidate(DraftB, revision: 1)),
            ]);
        await AssertPlanInvalid(composer, harness, shipping,
            [
                Plan("g1", [LineA], Candidate(DraftA, revision: 1), Candidate(DraftA, revision: 2, methodCode: "other")),
            ]);
        await AssertPlanInvalid(composer, harness, shipping,
            [Plan("g1", [LineA], Candidate(DraftA, revision: 1), Candidate(DraftA, revision: 1))]);
        await AssertPlanInvalid(composer, harness, shipping,
            [
                Plan("g1", [LineA],
                    Candidate(DraftA, revision: 1, ownerKind: ShippingCapturedQuoteOwnerKind.Seller, sellerId: null)),
            ]);
        await AssertPlanInvalid(composer, harness, shipping, []);
    }

    /// <summary>
    /// نبود/خرابی هفته، منطقهٔ زمانی و بازهٔ کاری هیچ تاریخ ساختگی نمی‌سازد؛ مبلغ معتبر باقی می‌ماند.
    /// </summary>
    [Fact]
    public async Task Missing_or_invalid_week_produces_no_dates_with_explicit_reason()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);

        var missing = await ComposeSingle(composer, harness, shipping, request => Quoted(request, 1_000m, 0m, week: null));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.ReceivingWeekMissing, missing.Receiving!.UnavailableReason);
        Assert.False(missing.Receiving.HasAvailableDates);

        var badZone = await ComposeSingle(
            composer, harness, shipping,
            request => Quoted(request, 1_000m, 0m, week: Week("Not/AZone", (1, 9, 0, 12, 0))));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.ReceivingTimeZoneInvalid, badZone.Receiving!.UnavailableReason);

        var badInterval = await ComposeSingle(
            composer, harness, shipping,
            request => Quoted(request, 1_000m, 0m, week: Week("UTC", (1, 12, 0, 9, 0))));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.ReceivingWeekInvalid, badInterval.Receiving!.UnavailableReason);

        var allClosed = await ComposeSingle(
            composer, harness, shipping,
            request => Quoted(request, 1_000m, 0m, week: Week("UTC")));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.NoWorkingDays, allClosed.Receiving!.UnavailableReason);
        Assert.Null(allClosed.Receiving.EarliestEligibleDate);

        var missingMinimum = await ComposeSingle(
            composer, harness, shipping,
            request => Quoted(request, 1_000m, 0m, week: Week("UTC"), minDays: null));
        Assert.Equal(
            StorefrontCapturedGroupProjectionReasons.MinimumDeliveryDaysInvalid,
            missingMinimum.Receiving!.UnavailableReason);

        // در همهٔ این حالت‌ها قیمت پولی معتبر بود و تقویم جدا گزارش شد.
        Assert.All(
            new[] { missing, badZone, badInterval, allClosed, missingMinimum },
            candidate => Assert.Equal(StorefrontCapturedCandidateOutcome.Quoted, candidate.Outcome));
    }

    /// <summary>
    /// خطای واقعی درز بیرونی و توکن لغو فراخوان دست‌نخورده می‌مانند؛ هیچ تلاش دوباره/نوشتنی نیست.
    /// </summary>
    [Fact]
    public async Task Caller_token_and_dependency_failure_are_propagated_without_retry_or_write()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);
        using var cancellation = new CancellationTokenSource();
        var failure = new InvalidOperationException("owner transport down");
        shipping.Failure = failure;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [Plan("g1", [LineA], Candidate(DraftA, revision: 1))]),
            cancellation.Token));

        Assert.Same(failure, thrown);
        Assert.Equal(1, shipping.Calls);
        Assert.Equal(cancellation.Token, shipping.LastToken);
        Assert.Equal(cancellation.Token, harness.Promotions.LastToken);
        Assert.Equal(cancellation.Token, harness.Taxes.LastToken);
        Assert.Equal(1, harness.Carts.Calls);

        // هیچ خواندن/نوشتنی روی OrderDbContext انجام نشد (provider اصلاً پیکربندی نشده است).
        Assert.Throws<InvalidOperationException>(() => harness.Db.Checkouts.Any());
    }

    /// <summary>یک نمونهٔ scoped برای ترکیب؛ همان نمونهٔ 217 برای فکت‌ها.</summary>
    [Fact]
    public void Module_registers_one_scoped_composer_over_the_same_facts_reader()
    {
        var services = new ServiceCollection();
        new OrderModule().AddServices(services, new EmptyTestConfiguration(), new FakeHostEnvironment());
        services.AddScoped<ICheckoutDirectory>(_ => new ProbeCheckoutDirectory());
        services.AddScoped<Tooba.Shipping.Contracts.Rates.IShippingCapturedRevisionQuoteLookup>(
            _ => new RecordingCapturedQuoteLookup());
        services.AddScoped<IClock>(_ => new MutableClock(DateTimeOffset.UtcNow));

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(StorefrontCapturedGroupProjectionComposer)));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var composer = scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupProjectionComposer>();
        Assert.NotNull(composer);
        var factsReader = scope.ServiceProvider.GetRequiredService<ICheckoutShippingGroupGoodsFactsReader>();
        var directory = scope.ServiceProvider.GetRequiredService<ICheckoutDirectory>();
        Assert.Same(directory, factsReader);
        Assert.Same(
            composer,
            scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupProjectionComposer>());
    }

    private static StorefrontCapturedGroupProjectionComposer Create(
        ShippingGroupGoodsFactsHarness harness,
        RecordingCapturedQuoteLookup shipping) =>
        new(harness.Directory, shipping, new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero)));

    private static async Task<StorefrontCapturedCandidateProjection> ComposeSingle(
        StorefrontCapturedGroupProjectionComposer composer,
        ShippingGroupGoodsFactsHarness harness,
        RecordingCapturedQuoteLookup shipping,
        Func<ShippingCapturedQuoteLookupRequest, ShippingCapturedQuoteResult?> respond)
    {
        shipping.Respond = respond;
        var result = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [Plan("g1", [LineA], Candidate(DraftA, revision: 4))]),
            CancellationToken.None);
        return Assert.Single(Assert.Single(result.Groups).Candidates);
    }

    private static async Task AssertPlanInvalid(
        StorefrontCapturedGroupProjectionComposer composer,
        ShippingGroupGoodsFactsHarness harness,
        RecordingCapturedQuoteLookup shipping,
        IReadOnlyList<StorefrontCapturedGroupPlan> groups)
    {
        var error = await Assert.ThrowsAsync<ContractOperationException>(() => composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(harness.Command(), groups),
            CancellationToken.None));
        Assert.Equal(StorefrontCapturedGroupProjectionReasons.PlanInvalid, error.Code);
        Assert.Equal(0, shipping.Calls);
        Assert.Equal(0, harness.Carts.Calls);
    }

    private static StorefrontCapturedGroupPlan Plan(
        string groupKey,
        IReadOnlyList<Guid> cartLineIds,
        params StorefrontCapturedCandidatePlan[] candidates) =>
        new(groupKey, cartLineIds, candidates);

    private static StorefrontCapturedCandidatePlan Candidate(
        Guid draftId,
        long revision,
        string methodCode = "express",
        int availableDateCount = 2,
        ShippingCapturedQuoteOwnerKind ownerKind = ShippingCapturedQuoteOwnerKind.Store,
        Guid? sellerId = null,
        string serviceCode = "express",
        string? optionCode = "std",
        string? tenantId = "tenant-a") =>
        new(
            methodCode,
            ownerKind,
            tenantId,
            sellerId,
            draftId,
            revision,
            new DestinationIdentifiers(CountryId, AdministrativeAreaId: null, CityId: CityId),
            serviceCode,
            optionCode,
            availableDateCount);

    private static ShippingCapturedQuoteResult Quoted(
        ShippingCapturedQuoteLookupRequest request,
        decimal freight,
        decimal packaging,
        string? serviceCode = null,
        string? optionCode = null,
        Guid? draftId = null,
        long? sourceRevision = null,
        string? currency = null,
        int? minDays = 2,
        int? maxDays = 4,
        ShippingCapturedReceivingWeek? week = null,
        Guid? groupId = null,
        Guid? rateRowId = null)
    {
        var total = freight + packaging;
        return new ShippingCapturedQuoteResult(
            ShippingCapturedQuoteVerdict.Quoted,
            ShippingCapturedQuoteVerdictCodes.Quoted,
            serviceCode ?? "express",
            optionCode ?? "std",
            currency ?? request.CurrencyCode,
            freight,
            packaging,
            total,
            total,
            0m,
            new ShippingCapturedQuoteSettings(
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual),
            SnapshotId: Guid.NewGuid(),
            DraftId: draftId ?? request.DraftId,
            SourceRevision: sourceRevision ?? request.SourceRevision,
            FormatVersion: 1,
            ShippingTypeId: Guid.Parse("0199f777-0000-7000-8000-000000000001"),
            GroupId: groupId ?? Guid.Parse("0199f777-0000-7000-8000-000000000002"),
            RateRowId: rateRowId ?? RowA,
            MinDeliveryDays: minDays,
            MaxDeliveryDays: maxDays,
            ReceivingWeek: week);
    }

    /// <summary>
    /// هفته با روزهای کاری مشخص (ISO ۱..۷) و ساعت همان روز؛ بقیهٔ روزها تعطیل‌اند.
    /// </summary>
    private static ShippingCapturedReceivingWeek Week(
        string timeZoneId,
        params (int Weekday, int OpenHour, int OpenMinute, int CloseHour, int CloseMinute)[] working)
    {
        var open = working.ToDictionary(x => x.Weekday);
        var days = Enumerable.Range(1, 7)
            .Select(iso => open.TryGetValue(iso, out var span)
                ? new ShippingCapturedReceivingWeekDay(
                    iso,
                    true,
                    new TimeOnly(span.OpenHour, span.OpenMinute),
                    new TimeOnly(span.CloseHour, span.CloseMinute))
                : new ShippingCapturedReceivingWeekDay(iso, false, null, null))
            .ToArray();
        return new ShippingCapturedReceivingWeek(timeZoneId, days);
    }
}
