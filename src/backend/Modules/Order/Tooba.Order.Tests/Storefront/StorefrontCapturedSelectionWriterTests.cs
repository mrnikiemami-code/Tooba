using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Tests.Checkout;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232 — اثبات متمرکز نویسندهٔ اتمیک انتخاب capture‌شده.
/// </summary>
/// <remarks>
/// <para>
/// این کلاس همان مسیر <b>واقعی</b> خواندن را اجرا می‌کند: انتساب دسته‌ای Offer، اشاره‌گر انتخاب‌شدهٔ ۲۱۹،
/// یک ترکیب ۲۱۸ با فکت‌های واقعی ۲۱۷ و نقل‌قول کنترل‌شدهٔ مالک. سپس قصد نامعتمد کلاینت را به همان
/// <c>ResolveAsync</c> مشترک می‌دهد؛ پس هیچ بازمحاسبه/توازی قیمت در مسیر نوشتن وجود ندارد.
/// </para>
/// <para>
/// حقیقت CAS واقعی روی پایگاه‌داده در <c>StorefrontCapturedSelectionStorageTests</c> اثبات می‌شود؛ این‌جا
/// فقط مقدارِ تحویل‌شده به درز ذخیره‌سازی و شکل شاهد/گروه/مقصد بررسی می‌شود.
/// </para>
/// </remarks>
[Collection("ShippingGroupGoodsFacts")]
public sealed class StorefrontCapturedSelectionWriterTests
{
    private static readonly Guid StoreTypeId = Guid.Parse("01a1785e-01f3-7000-b50d-d6372e756f89");
    private static readonly Guid OfferA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid OfferB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid LineA = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid LineB = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
    private static readonly Guid LanguageId = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");
    private static readonly Guid DraftA = Guid.Parse("aaaaaaaa-0000-4000-8000-00000000000a");
    private static readonly Guid AreaGroup = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly Guid RateRow = Guid.Parse("20000000-0000-4000-8000-000000000002");
    private static readonly Guid AreaGroupB = Guid.Parse("10000000-0000-4000-8000-0000000000b1");
    private static readonly Guid RateRowB = Guid.Parse("20000000-0000-4000-8000-0000000000b2");
    private static readonly Guid Snapshot = Guid.Parse("30000000-0000-4000-8000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Disabled_writer_refuses_captured_intent_before_any_read_or_write()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var drafts = new DraftStoreFake();
        var geography = new GeographyFake();
        var writer = Writer(harness, drafts, geography, selectionEnabled: false);

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            Cart(harness, LineA, OfferA),
            new CartAccess(harness.UserId, null),
            Prepared(),
            existing: null,
            guestSecretHash: null,
            Intent(GroupKey(harness), "post", "standard", "2026-10-29"),
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.Disabled, error.Code);
        Assert.Equal(0, drafts.ReplaceCalls);
        Assert.Equal(0, geography.Calls);
    }

    [Fact]
    public async Task Empty_intent_is_refused()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var writer = Writer(harness, new DraftStoreFake(), new GeographyFake());

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            Cart(harness, LineA, OfferA),
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            Intent(GroupKey(harness), "post", "standard", "2026-10-29", groups: []),
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.IntentInvalid, error.Code);
    }

    /// <summary>هر shorthand قدیمی در حضور قصد capture مبهم است و به مسیر قدیمی نمی‌افتد.</summary>
    [Fact]
    public async Task Legacy_shorthand_next_to_captured_intent_is_refused()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var drafts = new DraftStoreFake();
        var writer = Writer(harness, drafts, new GeographyFake());

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            Cart(harness, LineA, OfferA),
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            Intent(GroupKey(harness), "post", "standard", "2026-10-29") with
            {
                ShippingMethodCode = "post",
            },
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.IntentInvalid, error.Code);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    /// <summary>متن نسخهٔ نامعتبر قصد است (نه تضاد): شکل نامعتبر پیش از هر خواندن گران رد می‌شود.</summary>
    [Fact]
    public async Task Malformed_expected_revision_is_refused_as_invalid_intent()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var writer = Writer(harness, new DraftStoreFake(), new GeographyFake());

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            Cart(harness, LineA, OfferA),
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            Intent(GroupKey(harness), "post", "standard", "2026-10-29") with { ExpectedSelectionRevision = "3.5" },
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.IntentInvalid, error.Code);
    }

    [Fact]
    public async Task Unresolved_destination_is_refused_before_any_quote()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var drafts = new DraftStoreFake();
        var geography = new GeographyFake { Result = DestinationResolution.Unresolved() };
        var writer = Writer(harness, drafts, geography);

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            Cart(harness, LineA, OfferA),
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            Intent(GroupKey(harness), "post", "standard", "2026-10-29"),
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.DestinationUnverified, error.Code);
        Assert.Equal(1, geography.Calls);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    /// <summary>
    /// مسیر مثبت واقعی: یک گروه قیمت‌دار، شاهد ۲۳۱ از همان نقل‌قول، هویت مقصد تأییدشده و تنها
    /// <b>یک</b> جایگزینی اتمیک با نسخهٔ ۰ (یعنی «هیچ انتخاب ثبت‌شده‌ای»).
    /// </summary>
    [Fact]
    public async Task New_draft_writes_one_evidence_group_with_verified_destination_and_zero_revision()
    {
        using var harness = Positive(OfferA, LineA);
        var drafts = new DraftStoreFake();
        var geography = new GeographyFake();
        var planner = PositivePlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, geography, new FixedClock232());

        var cart = Cart(harness, LineA, OfferA);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);

        var draft = await writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            existing: null,
            guestSecretHash: null,
            intent,
            CancellationToken.None);

        Assert.Equal(1, drafts.ReplaceCalls);
        Assert.True(drafts.LastIsNew);
        Assert.Equal(0, drafts.LastExpectedRevision);
        Assert.Equal(1, geography.Calls);

        var group = Assert.Single(draft.ShippingGroups);
        Assert.Equal(group.ShippingGroupId, Assert.Single(drafts.LastGroups).ShippingGroupId);
        var evidence = group.AcceptedEvidence;
        Assert.NotNull(evidence);
        Assert.Equal(Snapshot, evidence!.SnapshotId);
        Assert.Equal(DraftA, evidence.DraftId);
        Assert.Equal(3, evidence.SourceRevision);
        Assert.Equal(1, evidence.FormatVersion);
        Assert.Equal(1, evidence.SelectionVersion);
        Assert.Equal(ShippingGroupOwnerKind.Store, evidence.Owner.Kind);
        Assert.Equal("store-alpha", evidence.Owner.TenantKey);
        Assert.Equal(AreaGroup, evidence.AreaGroupId);
        Assert.Equal(RateRow, evidence.RateRowId);
        Assert.Equal(15_000m, evidence.FreightAmount);
        Assert.Equal(2_000m, evidence.PackagingAmount);
        Assert.Equal(17_000m, evidence.PayableAtCheckoutAmount);
        Assert.Equal(0m, evidence.CarrierCollectAmount);
        Assert.Equal("IRR", evidence.Currency);
        Assert.Equal(2, evidence.MinimumDeliveryDays);
        Assert.Equal(4, evidence.MaximumDeliveryDays);
        Assert.Equal("Asia/Tehran", evidence.ReceivingTimeZone);
        Assert.Equal(group.SelectedDeliveryDate, evidence.ReceivingDate);

        // مقصد پایدار همان چیزی است که Geography تأیید کرد، نه متنی از مرورگر.
        Assert.Equal(GeographyFake.Country, draft.DestinationCountryId);
        Assert.Equal(GeographyFake.Area, draft.DestinationAdministrativeAreaId);
        Assert.Equal(GeographyFake.City, draft.DestinationCityId);
        Assert.Null(draft.DestinationServiceAreaId);
    }

    /// <summary>
    /// TB-232-R1: قصد کوپن در این برش fail-closed است — پیش از هر نقل‌قول/نوشتنی رد می‌شود و هیچ
    /// مبلغ کالای دیگری زیر همان توکن‌های بازتاب ذخیره نمی‌شود.
    /// </summary>
    [Fact]
    public async Task Nonblank_coupon_is_refused_before_any_quote_or_write()
    {
        using var harness = Positive(OfferA, LineA);
        var drafts = new DraftStoreFake();
        var planner = PositivePlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);

        var quoteCallsBefore = harness.Promotions.Calls;
        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            existing: null,
            guestSecretHash: null,
            intent with { CouponCode = "  SUMMER-232  " },
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.CouponUnavailable, error.Code);
        // هیچ نقل‌قول/تخفیفی برای این درخواست انجام نشده و هیچ نوشتنی رخ نداده است.
        Assert.Equal(quoteCallsBefore, harness.Promotions.Calls);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    /// <summary>کوپن خالی/فاصله همان «بدون کوپن» است و مسیر عادی را نمی‌بندد.</summary>
    [Fact]
    public async Task Blank_coupon_takes_the_ordinary_path()
    {
        using var harness = Positive(OfferA, LineA);
        var drafts = new DraftStoreFake();
        var planner = PositivePlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);

        await writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            existing: null,
            guestSecretHash: null,
            intent with { CouponCode = "   " },
            CancellationToken.None);

        Assert.Equal(1, drafts.ReplaceCalls);
    }

    /// <summary>پیش‌نویس موجود با نسخهٔ مشاهده‌شده: همان نسخه به‌عنوان CAS تحویل می‌شود.</summary>
    [Fact]
    public async Task Existing_draft_forwards_the_observed_selection_revision_to_the_atomic_store()
    {
        using var harness = Positive(OfferA, LineA);
        var drafts = new DraftStoreFake();
        var planner = PositivePlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA);
        var existing = ExistingDraft(harness, revisions: 2);
        var intent = await IntentFromPreviewAsync(harness, planner, cart, expectedRevision: "2");

        var draft = await writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            existing,
            guestSecretHash: null,
            intent,
            CancellationToken.None);

        Assert.Equal(1, drafts.ReplaceCalls);
        Assert.False(drafts.LastIsNew);
        Assert.Equal(2, drafts.LastExpectedRevision);
        Assert.Same(existing, draft);
        Assert.Equal(3, draft.SelectionRevision);
        Assert.All(draft.ShippingGroups, group => Assert.NotNull(group.AcceptedEvidence));
    }

    /// <summary>پاسخ مالک/اشاره‌گر عوض شده است: هیچ نوشتنی انجام نمی‌شود.</summary>
    [Fact]
    public async Task Changed_owner_echo_is_a_conflict_and_writes_nothing()
    {
        using var harness = Positive(OfferA, LineA);
        var drafts = new DraftStoreFake();
        var planner = PositivePlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);
        var group = Assert.Single(intent.CapturedGroups!);
        var tampered = intent with
        {
            CapturedGroups = [group with { ExpectedRateRowId = Guid.NewGuid().ToString("D") }],
        };

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            tampered,
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.Conflict, error.Code);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    [Fact]
    public async Task Date_outside_the_group_calendar_is_refused()
    {
        using var harness = Positive(OfferA, LineA);
        var drafts = new DraftStoreFake();
        var planner = PositivePlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);
        var group = Assert.Single(intent.CapturedGroups!);
        var tampered = intent with
        {
            CapturedGroups = [group with { SelectedDeliveryDate = "2099-01-01" }],
        };

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            tampered,
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.DateUnavailable, error.Code);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    /// <summary>هر گروه سرور باید انتخاب شود؛ پوشش جزئی کل نوشتن را می‌بندد.</summary>
    [Fact]
    public async Task Partial_group_coverage_is_refused()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var drafts = new DraftStoreFake();
        var planner = TwoGroupPlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA, LineB, OfferB);
        var full = await IntentFromPreviewAsync(harness, planner, cart);
        Assert.Equal(2, full.CapturedGroups!.Count);

        var partial = full with { CapturedGroups = [full.CapturedGroups[0]] };
        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            partial,
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.IntentInvalid, error.Code);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    [Fact]
    public async Task Duplicate_group_key_is_refused()
    {
        using var harness = Positive(OfferA, LineA);
        var drafts = new DraftStoreFake();
        var planner = PositivePlanner(harness);
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);
        var group = Assert.Single(intent.CapturedGroups!);
        var duplicated = intent with { CapturedGroups = [group, group] };

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            duplicated,
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.IntentInvalid, error.Code);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    /// <summary>این برش فقط مالک Store را می‌نویسد؛ مالک دیگر هرگز ذخیره نمی‌شود.</summary>
    [Fact]
    public async Task Marketplace_owner_is_never_written_by_this_slice()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = new OfferFulfillmentAssignmentSnapshot(
            OfferA,
            harness.SellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        var planner = new StorefrontCapturedPreviewPlanner(
            assignments,
            new CatalogFake("post", "standard"),
            ActiveTypeFake(),
            new SelectedCaptureFake(),
            Composer(harness),
            new EditionFake(ToobaEdition.Marketplace),
            new TenantFake("store-alpha"),
            Options.Create(new StorefrontCapturedPreviewOptions
            {
                Enabled = true,
                SelectionEnabled = true,
                AvailableDateCount = 7,
            }));

        var drafts = new DraftStoreFake();
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() => writer.SaveAsync(
            Cart(harness, LineA, OfferA),
            new CartAccess(harness.UserId, null),
            Prepared(),
            null,
            null,
            Intent("cap|platform|marketplace|" + harness.SellerId.ToString("N") + "|" + StoreTypeId.ToString("N"), "post", "standard", "2026-10-29"),
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedSelectionReasons.Unavailable, error.Code);
        Assert.Equal(0, drafts.ReplaceCalls);
    }

    /// <summary>
    /// TB-232-R1 (RED→GREEN): دو فروشندهٔ متفاوت با <b>همان</b> Binding سرویس/گزینهٔ Store دقیقاً یک
    /// MethodCode مشترک دارند. شاهد هر گروه باید متعلق به <b>همان تعهد</b> باشد: ردیف نرخ و ساعت دریافتی
    /// خودش — نه ردیف/تاریخ/ساعت گروه دیگر. مبلغ برابر است تا هیچ تفاوت قیمتی مچ‌گیری را پنهان نکند.
    /// </summary>
    [Fact]
    public async Task Two_sellers_sharing_one_store_binding_keep_their_own_evidence()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var planner = TwoGroupPlanner(harness, TwoSellerQuoteLookup(secondFreight: 15_000m));
        var drafts = new DraftStoreFake();
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA, LineB, OfferB);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);
        Assert.Equal(2, intent.CapturedGroups!.Count);

        await writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            existing: null,
            guestSecretHash: null,
            intent,
            CancellationToken.None);

        Assert.Equal(2, drafts.LastGroups.Count);
        var first = drafts.LastGroups.Single(group => group.SellerPartyId == harness.SellerId);
        var second = drafts.LastGroups.Single(group => group.SellerPartyId == harness.OtherSellerId);
        var firstEvidence = first.AcceptedEvidence!;
        var secondEvidence = second.AcceptedEvidence!;

        // فروشندهٔ اول: ناحیه/ردیف/ساعت خودش.
        Assert.Equal(AreaGroup, firstEvidence.AreaGroupId);
        Assert.Equal(RateRow, firstEvidence.RateRowId);
        Assert.Equal(new TimeOnly(9, 0).Ticks, firstEvidence.ReceivingOpenTicks);
        Assert.Equal(new TimeOnly(17, 0).Ticks, firstEvidence.ReceivingCloseTicks);
        Assert.Equal(first.SelectedDeliveryDate, firstEvidence.ReceivingDate);

        // فروشندهٔ دوم با همان MethodCode: ردیف/ساعت خودش، نه شاهد گروه اول.
        Assert.Equal(AreaGroupB, secondEvidence.AreaGroupId);
        Assert.Equal(RateRowB, secondEvidence.RateRowId);
        Assert.Equal(new TimeOnly(10, 0).Ticks, secondEvidence.ReceivingOpenTicks);
        Assert.Equal(new TimeOnly(14, 0).Ticks, secondEvidence.ReceivingCloseTicks);
        Assert.Equal(second.SelectedDeliveryDate, secondEvidence.ReceivingDate);

        Assert.NotEqual(firstEvidence.RateRowId, secondEvidence.RateRowId);
        Assert.NotEqual(firstEvidence.ReceivingOpenTicks, secondEvidence.ReceivingOpenTicks);
    }

    /// <summary>
    /// کنترل با مبلغ‌های متفاوت: تفاوت قیمت هم نوشتن را نمی‌شکند و هر گروه همان مبلغ خودش را نگه می‌دارد.
    /// </summary>
    [Fact]
    public async Task Two_sellers_with_different_prices_each_keep_their_own_money()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var planner = TwoGroupPlanner(harness, TwoSellerQuoteLookup(secondFreight: 90_000m));
        var drafts = new DraftStoreFake();
        var writer = new StorefrontCapturedSelectionWriter(planner, drafts, new GeographyFake(), new FixedClock232());

        var cart = Cart(harness, LineA, OfferA, LineB, OfferB);
        var intent = await IntentFromPreviewAsync(harness, planner, cart);

        await writer.SaveAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Prepared(),
            existing: null,
            guestSecretHash: null,
            intent,
            CancellationToken.None);

        var first = drafts.LastGroups.Single(group => group.SellerPartyId == harness.SellerId);
        var second = drafts.LastGroups.Single(group => group.SellerPartyId == harness.OtherSellerId);
        Assert.Equal(15_000m, first.AcceptedEvidence!.FreightAmount);
        Assert.Equal(2_000m, first.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(90_000m, second.AcceptedEvidence!.FreightAmount);
        Assert.Equal(2_000m, second.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(92_000m, second.AcceptedEvidence!.PayableAtCheckoutAmount);
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>
    /// همان Binding برای هر دو گروه (post/standard)، ولی ردیف نرخ و ساعت دریافتی هر فروشنده جداست؛
    /// انتخاب پاسخ بر پایهٔ مبلغ کالای همان گروه است تا هیچ‌کدام پاسخ دیگری را نگیرد.
    /// </summary>
    private static RecordingCapturedQuoteLookup TwoSellerQuoteLookup(decimal secondFreight) =>
        new()
        {
            Respond = request => (request.FinalGroupGoodsAmountAfterDiscountAndTax ?? 0m) >= 150m
                ? Quoted(
                    request,
                    secondFreight,
                    2_000m,
                    AreaGroupB,
                    RateRowB,
                    Week([6, 7], new TimeOnly(10, 0), new TimeOnly(14, 0)))
                : Quoted(
                    request,
                    15_000m,
                    2_000m,
                    AreaGroup,
                    RateRow,
                    Week([1, 2, 3, 4, 5], new TimeOnly(9, 0), new TimeOnly(17, 0))),
        };

    private static StorefrontCapturedSelectionWriter Writer(
        ShippingGroupGoodsFactsHarness harness,
        IStorefrontShippingDraftStore drafts,
        GeographyFake geography,
        bool selectionEnabled = true) =>
        new(PositivePlanner(harness, selectionEnabled), drafts, geography, new FixedClock232());

    private static StorefrontCapturedPreviewPlanner PositivePlanner(
        ShippingGroupGoodsFactsHarness harness,
        bool selectionEnabled = true)
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = new OfferFulfillmentAssignmentSnapshot(
            OfferA,
            harness.SellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        return new StorefrontCapturedPreviewPlanner(
            assignments,
            new CatalogFake("post", "standard"),
            ActiveTypeFake(),
            new SelectedCaptureFake(),
            Composer(harness),
            new EditionFake(ToobaEdition.SingleStore),
            new TenantFake("store-alpha"),
            Options.Create(new StorefrontCapturedPreviewOptions
            {
                Enabled = true,
                SelectionEnabled = selectionEnabled,
                AvailableDateCount = 7,
            }));
    }

    private static StorefrontCapturedPreviewPlanner TwoGroupPlanner(
        ShippingGroupGoodsFactsHarness harness,
        RecordingCapturedQuoteLookup? quoteLookup = null)
    {
        harness.Carts.Cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m),
            harness.Line(LineB, OfferB, 200m));
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = new OfferFulfillmentAssignmentSnapshot(
            OfferA,
            harness.SellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        // گروه دوم با فروشندهٔ دیگر و روش دیگر: یک نامزد مستقل، همان یک ترکیب.
        assignments.Values[OfferB] = new OfferFulfillmentAssignmentSnapshot(
            OfferB,
            harness.OtherSellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        var quote = quoteLookup ?? new RecordingCapturedQuoteLookup
        {
            Respond = request => Quoted(request, 15_000m, 2_000m),
        };
        return new StorefrontCapturedPreviewPlanner(
            assignments,
            new CatalogFake("post", "standard"),
            ActiveTypeFake(),
            new SelectedCaptureFake(),
            new StorefrontCapturedGroupProjectionComposer(harness.Directory, quote, new MutableClock(Now)),
            new EditionFake(ToobaEdition.SingleStore),
            new TenantFake("store-alpha"),
            Options.Create(new StorefrontCapturedPreviewOptions
            {
                Enabled = true,
                SelectionEnabled = true,
                AvailableDateCount = 7,
            }));
    }

    /// <summary>مسیر مثبت واقعی: فکت‌های ۲۱۷ واقعی، یک نقل‌قول کنترل‌شده و یک اشاره‌گر انتخاب‌شده.</summary>
    private static ShippingGroupGoodsFactsHarness Positive(Guid offerId, Guid lineId)
    {
        var harness = new ShippingGroupGoodsFactsHarness();
        harness.Promotions.Discounts[offerId] = 10m;
        harness.Carts.Cart = harness.Cart(harness.Line(lineId, offerId, 100m));
        return harness;
    }

    private static StorefrontCapturedGroupProjectionComposer Composer(ShippingGroupGoodsFactsHarness harness) =>
        new(
            harness.Directory,
            new RecordingCapturedQuoteLookup
            {
                Respond = request => Quoted(request, 15_000m, 2_000m),
            },
            new MutableClock(Now));

    /// <summary>
    /// قصد را از همان پیش‌نمایش واقعی می‌سازد: کلید گروه، روش، تاریخ و توکن‌های تأیید — نه اعداد ساختگی.
    /// </summary>
    private static async Task<StorefrontShippingSelectionRequest> IntentFromPreviewAsync(
        ShippingGroupGoodsFactsHarness harness,
        StorefrontCapturedPreviewPlanner planner,
        CartPage cart,
        string? expectedRevision = "0")
    {
        var preview = await planner.BuildAsync(
            cart,
            new CartAccess(harness.UserId, null),
            new DestinationIdentifiers(GeographyFake.Country, GeographyFake.Area, GeographyFake.City),
            "IRR",
            LanguageId,
            CancellationToken.None);

        Assert.Null(preview.NoticeReason);
        Assert.NotEmpty(preview.Groups);

        var groups = preview.Groups
            .Select(group =>
            {
                var method = Assert.Single(group.Methods);
                Assert.False(string.IsNullOrWhiteSpace(method.AvailableDates[0].Date));
                return new StorefrontCapturedGroupSelection(
                    group.GroupKey,
                    method.ServiceCode,
                    method.OptionCode,
                    method.AvailableDates[0].Date,
                    method.ConfirmationSnapshotId,
                    method.ConfirmationSourceRevision,
                    method.ConfirmationSelectionVersion,
                    method.ConfirmationAreaGroupId,
                    method.ConfirmationRateRowId);
            })
            .ToArray();

        return Intent(groups[0].GroupKey, groups[0].ServiceCode, groups[0].OptionCode, groups[0].SelectedDeliveryDate) with
        {
            CapturedGroups = groups,
            ExpectedSelectionRevision = expectedRevision,
        };
    }

    private static StorefrontShippingSelectionRequest Intent(
        string groupKey,
        string serviceCode,
        string? optionCode,
        string date,
        IReadOnlyList<StorefrontCapturedGroupSelection>? groups = null) =>
        new(
            Guid.Empty,
            7,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            ShippingGroups: null,
            CapturedGroups: groups ?? [new StorefrontCapturedGroupSelection(groupKey, serviceCode, optionCode, date)],
            ExpectedSelectionRevision: "0");

    private static string GroupKey(ShippingGroupGoodsFactsHarness harness) =>
        "cap|platform|store|" + harness.SellerId.ToString("N") + "|" + StoreTypeId.ToString("N");

    private static CartPage Cart(
        ShippingGroupGoodsFactsHarness harness,
        Guid lineA,
        Guid offerA,
        Guid? lineB = null,
        Guid? offerB = null)
    {
        var lines = new List<CartLineView>
        {
            Line(lineA, offerA, harness.SellerId),
        };
        if (lineB is Guid second && offerB is Guid secondOffer)
        {
            lines.Add(Line(second, secondOffer, harness.OtherSellerId));
        }

        return new CartPage(
            harness.CartId,
            7,
            "IR",
            "IRR",
            "Online",
            lines.Count,
            [new CartCurrencyTotal("IRR", 100m)],
            lines,
            "guest",
            "Active");
    }

    private static CartLineView Line(Guid lineId, Guid offerId, Guid sellerId) =>
        new(lineId, offerId, Guid.NewGuid(), sellerId, null, null, "کالا", "فروشنده", null, 1m, 100m, 100m, "IRR", false);

    private static StorefrontCheckoutPlacement Prepared() =>
        new(
            Guid.NewGuid(),
            new StorefrontCheckoutShippingInput(
                "گیرنده",
                "09120000000",
                "تهران",
                "تهران",
                "خیابان یک",
                "1234567890",
                null,
                "گیرنده",
                "آزمایشی"));

    private static CartShippingDraft ExistingDraft(ShippingGroupGoodsFactsHarness harness, int revisions)
    {
        var draft = CartShippingDraft.Create(
            harness.CartId,
            null,
            7,
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان یک",
            "1234567890",
            null,
            "post",
            "پست",
            17_000m,
            new DateOnly(2026, 10, 29),
            new DateOnly(2026, 10, 29),
            "09:00-12:00",
            null,
            Now);
        for (var index = 0; index < revisions; index++)
        {
            draft.BumpSelectionRevision(Now);
        }

        return draft;
    }

    private static ShippingCapturedQuoteResult Quoted(
        ShippingCapturedQuoteLookupRequest request,
        decimal freight,
        decimal packaging,
        Guid? areaGroup = null,
        Guid? rateRow = null,
        ShippingCapturedReceivingWeek? week = null) =>
        new(
            ShippingCapturedQuoteVerdict.Quoted,
            ShippingCapturedQuoteVerdictCodes.Quoted,
            "post",
            "standard",
            "IRR",
            freight,
            packaging,
            freight + packaging,
            freight + packaging,
            0m,
            new ShippingCapturedQuoteSettings(
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual),
            SnapshotId: Snapshot,
            DraftId: request.DraftId,
            SourceRevision: request.SourceRevision,
            FormatVersion: 1,
            ShippingTypeId: StoreTypeId,
            GroupId: areaGroup ?? AreaGroup,
            RateRowId: rateRow ?? RateRow,
            MinDeliveryDays: 2,
            MaxDeliveryDays: 4,
            ReceivingWeek: week ?? Week());

    private static ShippingCapturedReceivingWeek Week(
        int[]? openIsos = null,
        TimeOnly? opens = null,
        TimeOnly? closes = null)
    {
        var openDays = openIsos ?? [6, 7];
        var opensAt = opens ?? new TimeOnly(9, 0);
        var closesAt = closes ?? new TimeOnly(17, 0);
        var days = Enumerable.Range(1, 7)
            .Select(iso => openDays.Contains(iso)
                ? new ShippingCapturedReceivingWeekDay(iso, true, opensAt, closesAt)
                : new ShippingCapturedReceivingWeekDay(iso, false, null, null))
            .ToArray();
        return new ShippingCapturedReceivingWeek("Asia/Tehran", days);
    }

    private static TypeFake ActiveTypeFake()
    {
        var types = new TypeFake();
        types.Values[StoreTypeId] = new ShippingTypeSnapshot(
            StoreTypeId,
            "standard",
            true,
            10,
            2,
            [new ShippingTypeTranslationSnapshot(LanguageId, "نوع", null)]);
        return types;
    }

    private sealed class DraftStoreFake : IStorefrontShippingDraftStore
    {
        internal int ReplaceCalls { get; private set; }

        internal bool LastIsNew { get; private set; }

        internal long LastExpectedRevision { get; private set; }

        internal IReadOnlyList<CartShippingDraftGroup> LastGroups { get; private set; } = [];

        public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
            Task.FromResult<CartShippingDraft?>(null);

        public Task SaveAsync(CartShippingDraft value, bool isNew, CancellationToken cancellationToken) =>
            throw new NotSupportedException("the legacy save path must not be used by the captured writer");

        public Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken) =>
            GetByCartIdAsync(cartId, cancellationToken);

        public Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
            CartShippingDraft draft,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken)
        {
            ReplaceCalls++;
            LastIsNew = isNew;
            LastExpectedRevision = expectedSelectionRevision;

            if (isNew)
            {
                if (expectedSelectionRevision != 0)
                {
                    return Task.FromException<CartShippingDraft>(new StorefrontOrderException(StorefrontShippingSelectionConflict.Stale));
                }
            }
            else if (draft.SelectionRevision != expectedSelectionRevision)
            {
                return Task.FromException<CartShippingDraft>(new StorefrontOrderException(StorefrontShippingSelectionConflict.Stale));
            }

            LastGroups = groups;
            draft.BumpSelectionRevision(draft.UpdatedAt);
            return Task.FromResult(draft);
        }
    }

    private sealed class GeographyFake : IGeographyLookup
    {
        internal static readonly Guid Country = Guid.Parse("40000000-0000-4000-8000-000000000004");
        internal static readonly Guid Area = Guid.Parse("50000000-0000-4000-8000-000000000005");
        internal static readonly Guid City = Guid.Parse("60000000-0000-4000-8000-000000000006");

        internal int Calls { get; private set; }

        internal DestinationResolution Result { get; set; } =
            DestinationResolution.Resolved(new DestinationIdentifiers(Country, Area, City));

        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken) =>
            Task.FromResult<DestinationIdentifiers?>(identifiers);

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class AssignmentFake : IOfferFulfillmentAssignmentLookup
    {
        internal Dictionary<Guid, OfferFulfillmentAssignmentSnapshot> Values { get; } = [];

        public Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot> result = Values
                .Where(pair => offerIds.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            return Task.FromResult(result);
        }
    }

    private sealed class CatalogFake(string service, string option) : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>(
            [
                new ShippingCatalogServiceSnapshot(
                    Guid.NewGuid(),
                    service,
                    "Manual",
                    "post",
                    "primary",
                    true,
                    10,
                    [new ShippingCatalogTranslationSnapshot(LanguageId, service, null)],
                    [
                        new ShippingCatalogOptionSnapshot(
                            Guid.NewGuid(),
                            Guid.Empty,
                            option,
                            true,
                            10,
                            [new ShippingCatalogOptionTranslationSnapshot(LanguageId, option)]),
                    ]),
            ]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(
            Guid serviceId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TypeFake : IShippingTypeReader
    {
        internal Dictionary<Guid, ShippingTypeSnapshot> Values { get; } = [];

        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>(
                Values.Values.Where(type => type.IsActive).ToArray());

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>(
                Values.Values.Where(type => shippingTypeIds.Contains(type.ShippingTypeId)).ToArray());
    }

    private sealed class SelectedCaptureFake : IShippingSelectedCapturedRevisionLookup
    {
        public Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
            ShippingSelectedCapturedRevisionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ShippingSelectedCapturedRevisionResult(
                ShippingSelectedCaptureVerdict.Selected,
                null,
                ShippingSelectedCaptureReadState.Selected,
                1,
                Snapshot,
                DraftA,
                3,
                1,
                request.ServiceCode,
                request.OptionCode,
                StoreTypeId,
                [new ShippingSelectedCapturedTranslation(LanguageId, "پست", null)]));
    }

    private sealed class EditionFake(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current { get; } = new(edition, "test");
    }

    private sealed class TenantFake(string tenantId) : ICurrentTenant
    {
        public TenantContext? Current { get; } = new(
            new TenantId(tenantId),
            TenantStatus.Active,
            new ConnectionReference("test"),
            null,
            null,
            null,
            "store.local",
            null);
    }

    private sealed class FixedClock232 : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
