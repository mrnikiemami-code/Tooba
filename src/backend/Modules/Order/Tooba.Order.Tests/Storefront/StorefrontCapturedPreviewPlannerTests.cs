using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Tests.Checkout;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-229: اثبات متمرکز مسیر واقعی خواندنی «سبد → انتساب Offer → نامزد ۲۱۹ → یک ترکیب ۲۱۸».
/// </summary>
/// <remarks>
/// <para>
/// این کلاس فقط <b>قبل از ترکیب</b> را اجرا می‌کند: کیفیت افراز، رد شدن آفر بدون مسئولیت/نوع، محدودیت
/// مالک Seller در SingleStore، یک‌بار بودن خواندن دسته‌ای آفر و یک‌بار بودن خواندن ۲۱۹ برای هر
/// مالک+Binding. خود ترکیب ۲۱۸ و قیمت ۲۱۶/فکت ۲۱۷ در کلاس‌های همان برش‌ها پوشش داده شده‌اند و اینجا
/// تکرار نمی‌شوند؛ برای همین `FixedGoodsFactsReader(null)` در صورت فراخوانی صریحاً شکست می‌دهد.
/// </para>
/// </remarks>
[Collection("ShippingGroupGoodsFacts")]
public class StorefrontCapturedPreviewPlannerTests
{
    private static readonly Guid StoreTypeId = Guid.Parse("01a1785e-01f3-7000-b50d-d6372e756f89");
    private static readonly Guid SellerA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SellerB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OfferA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OfferB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid LineA = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid LineB = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid LanguageId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid DraftA = Guid.Parse("aaaaaaaa-0000-4000-8000-00000000000a");
    private static readonly Guid DraftB = Guid.Parse("bbbbbbbb-0000-4000-8000-00000000000b");
    private static readonly DestinationIdentifiers Destination = new(
        CountryId: Guid.Parse("99999999-9999-9999-9999-999999999999"));

    [Fact]
    public async Task Disabled_option_reads_nothing_and_reports_not_enabled()
    {
        var assignments = new AssignmentFake();
        var planner = Planner(
            enabled: false,
            assignments: assignments,
            catalog: new CatalogFake(),
            types: new TypeFake(),
            selected: new SelectedCaptureFake());

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        Assert.False(result.Enabled);
        Assert.Empty(result.Groups);
        Assert.Null(result.NoticeReason);
        Assert.Equal(0, assignments.Calls);
    }

    [Fact]
    public async Task Missing_assignment_fails_closed_without_any_catalog_or_pointer_read()
    {
        var assignments = new AssignmentFake();
        var catalog = new CatalogFake();
        var selected = new SelectedCaptureFake();
        var planner = Planner(true, assignments, catalog, new TypeFake(), selected);

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        Assert.True(result.Enabled);
        Assert.Equal(StorefrontCapturedPreviewReasons.AssignmentMissing, result.NoticeReason);
        Assert.Empty(result.Groups);
        Assert.Equal(0, catalog.Calls);
        Assert.Equal(0, selected.Calls);
    }

    [Fact]
    public async Task Assignment_of_another_seller_is_contradictory_and_never_reinterpreted()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(OfferA, SellerB, OfferFulfillmentResponsibility.Platform, StoreTypeId);
        var planner = Planner(true, assignments, new CatalogFake(), new TypeFake(), new SelectedCaptureFake());

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        Assert.Equal(StorefrontCapturedPreviewReasons.AssignmentContradictory, result.NoticeReason);
        Assert.Empty(result.Groups);
    }

    [Fact]
    public async Task Offer_without_explicit_responsibility_stays_visible_and_unavailable()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(OfferA, SellerA, responsibility: null, shippingTypeId: null);
        var planner = Planner(true, assignments, new CatalogFake(), new TypeFake(), new SelectedCaptureFake());

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        var group = Assert.Single(result.Groups);
        Assert.Equal(StorefrontCapturedPreviewReasons.Unassigned, group.UnavailableReason);
        Assert.Empty(group.Methods);
        Assert.Equal(SellerA, group.SellerPartyId);
        Assert.Equal([LineA], group.CartLineIds);
    }

    [Fact]
    public async Task Inactive_assigned_type_is_unavailable_rather_than_guessed()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(OfferA, SellerA, OfferFulfillmentResponsibility.Platform, StoreTypeId);
        var types = new TypeFake();
        types.Values[StoreTypeId] = Type(StoreTypeId, active: false);
        var planner = Planner(true, assignments, new CatalogFake(), types, new SelectedCaptureFake());

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        var group = Assert.Single(result.Groups);
        Assert.Equal(StorefrontCapturedPreviewReasons.TypeUnavailable, group.UnavailableReason);
    }

    /// <summary>در SingleStore مسئولیت Seller پشتیبانی نمی‌شود و هرگز به Store بازتفسیر نمی‌شود.</summary>
    [Fact]
    public async Task Seller_responsibility_on_single_store_is_unavailable_not_store_owner()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(OfferA, SellerA, OfferFulfillmentResponsibility.Seller, StoreTypeId);
        var selected = new SelectedCaptureFake();
        var planner = Planner(
            true,
            assignments,
            new CatalogFake(),
            new TypeFake(),
            selected,
            edition: ToobaEdition.SingleStore,
            tenantId: "store-alpha");

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        var group = Assert.Single(result.Groups);
        Assert.Equal(StorefrontCapturedPreviewReasons.SellerOwnerUnsupported, group.UnavailableReason);
        Assert.Equal(0, selected.Calls);
    }

    [Fact]
    public async Task Platform_group_reads_pointer_once_per_distinct_binding_and_never_composes_without_candidates()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(OfferA, SellerA, OfferFulfillmentResponsibility.Platform, StoreTypeId);
        var catalog = new CatalogFake();
        catalog.Services =
        [
            Service("post", "standard", "express"),
        ];
        var selected = new SelectedCaptureFake();
        var goods = new FixedGoodsFactsReader(null);
        var types = ActiveTypeFake();
        var planner = Planner(true, assignments, catalog, types, selected, goodsFacts: goods);

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        // Active options + null-option probe ⇒ three 219 reads for the same owner (no Selected).
        Assert.Equal(3, selected.Calls);
        Assert.Contains(selected.Requests, request => request.OptionCode is null);
        Assert.Contains(selected.Requests, request => request.OptionCode == "standard");
        Assert.Contains(selected.Requests, request => request.OptionCode == "express");
        Assert.All(selected.Requests, request =>
        {
            Assert.Equal(ShippingCapturedQuoteOwnerKind.Store, request.OwnerKind);
            Assert.Equal("store-alpha", request.OwnerTenantId);
            Assert.Null(request.OwnerSellerPartyId);
        });
        Assert.Equal(1, assignments.Calls);
        Assert.Equal(0, goods.Calls);

        var group = Assert.Single(result.Groups);
        Assert.Equal(StorefrontCapturedPreviewReasons.NoSelectedCapture, group.UnavailableReason);
    }

    /// <summary>مالک/نوع/بازبینی ناسازگار هرگز نامزد نمی‌سازد.</summary>
    [Fact]
    public async Task Selected_capture_of_a_different_type_is_not_a_candidate()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(OfferA, SellerA, OfferFulfillmentResponsibility.Platform, StoreTypeId);
        var catalog = new CatalogFake();
        catalog.Services = [Service("post", "standard")];
        var otherType = Guid.Parse("01a1785e-6b50-7000-a98f-2e1c80544815");
        var selected = new SelectedCaptureFake();
        selected.Respond = request => request.OptionCode is null
            ? Absent()
            : Selected(
                request,
                shippingTypeId: otherType,
                draftId: Guid.NewGuid(),
                revision: 4);
        var goods = new FixedGoodsFactsReader(null);
        var planner = Planner(true, assignments, catalog, ActiveTypeFake(), selected, goodsFacts: goods);

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        var group = Assert.Single(result.Groups);
        Assert.Equal(StorefrontCapturedPreviewReasons.NoSelectedCapture, group.UnavailableReason);
        Assert.Equal(0, goods.Calls);
    }

    [Fact]
    public async Task Clear_and_absent_pointers_are_not_candidates()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(OfferA, SellerA, OfferFulfillmentResponsibility.Platform, StoreTypeId);
        var catalog = new CatalogFake();
        catalog.Services = [Service("post", "standard")];
        var selected = new SelectedCaptureFake
        {
            Respond = request => new ShippingSelectedCapturedRevisionResult(
                ShippingSelectedCaptureVerdict.Cleared,
                null,
                ShippingSelectedCaptureReadState.Cleared,
                3,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                []),
        };
        var goods = new FixedGoodsFactsReader(null);
        var planner = Planner(true, assignments, catalog, ActiveTypeFake(), selected, goodsFacts: goods);

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        Assert.Equal(StorefrontCapturedPreviewReasons.NoSelectedCapture, Assert.Single(result.Groups).UnavailableReason);
        Assert.Equal(0, goods.Calls);
    }

    /// <summary>
    /// TB-229-R1: ترکیب مثبت واقعی — دو گروه با مبلغ کالای ۹۹/۱۹۸ از مسیر واقعی ۲۱۷، کرایه و هفتهٔ
    /// جدا، و یک گروه سوم بدون مسئولیت که دیده می‌شود و گروه‌های قیمت‌دار را پاک نمی‌کند.
    /// </summary>
    [Fact]
    public async Task Two_groups_compose_real_goods_amounts_with_independent_fees_and_one_unassigned_group()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        harness.Promotions.Discounts[OfferA] = 10m;
        harness.Promotions.Discounts[OfferB] = 20m;

        var assignments = new AssignmentFake();
        // فروشنده‌های Harness: افراز/فکت‌های واقعی ۲۱۷ با همین هویت‌ها ساخته می‌شوند.
        var sellerA = harness.SellerId;
        var sellerB = harness.OtherSellerId;
        assignments.Values[OfferA] = Assignment(OfferA, sellerA, OfferFulfillmentResponsibility.Platform, StoreTypeId);
        assignments.Values[OfferB] = Assignment(OfferB, sellerB, OfferFulfillmentResponsibility.Platform, StoreTypeId);

        var catalog = new CatalogFake();
        catalog.Services = [Service("post", "standard")];
        var shipping = new RecordingCapturedQuoteLookup();
        var week = Week("Asia/Tehran", (6, 9, 0, 12, 0), (7, 10, 15, 13, 45));
        // کرایه/بسته‌بندی هر گروه از نقل‌قول همان گروه می‌آید؛ دو گروه حتی با یک تعهد مشترک هم
        // مبلغ مستقل خودشان را دارند (مبلغ گروه از فکت ۲۱۷، نه از برچسب روش).
        shipping.Respond = request => request.FinalGroupGoodsAmountAfterDiscountAndTax == 99m
            ? Quoted(request, freight: 15_000m, packaging: 2_000m, week: week)
            : Quoted(request, freight: 25_000m, packaging: 0m, week: week);

        var selected = new SelectedCaptureFake
        {
            // هر دو گروه Platform/Store با یک Binding کاتالوگ‌اند: یک انتخاب سرور، نه دو.
            // Null-option is probed but has no Selected capture here.
            Respond = request => request.OptionCode is null
                ? Absent()
                : Selected(request, StoreTypeId, DraftA, revision: 3),
        };

        var planner = Planner(
            true,
            assignments,
            catalog,
            ActiveTypeFake(),
            selected,
            edition: ToobaEdition.SingleStore,
            composer: new StorefrontCapturedGroupProjectionComposer(
                harness.Directory,
                shipping,
                new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero))));

        // یک خط سوم با آفر بدون مسئولیت: گروهش باید زنده بماند.
        var unassignedLine = Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff");
        var unassignedOffer = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");
        assignments.Values[unassignedOffer] = Assignment(
            unassignedOffer,
            sellerA,
            responsibility: null,
            shippingTypeId: null);
        harness.Carts.Cart = harness.Cart(
            harness.Line(LineA, OfferA, 100m, sellerPartyId: sellerA),
            harness.Line(LineB, OfferB, 200m, sellerPartyId: sellerB),
            harness.Line(unassignedLine, unassignedOffer, 50m, sellerPartyId: sellerA));

        var cart = CartFor(
            harness.CartId,
            Line(LineA, OfferA, sellerA),
            Line(LineB, OfferB, sellerB),
            Line(unassignedLine, unassignedOffer, sellerA));

        var result = await planner.BuildAsync(
            cart,
            new CartAccess(harness.UserId, null),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        Assert.True(result.Enabled);
        Assert.Equal(3, result.Groups.Count);
        Assert.Null(result.NoticeReason);
        // یک نقل‌قول برای هر نامزد همان گروه‌ها؛ گروه بدون مسئولیت هیچ نقل‌قولی نمی‌خواهد.
        Assert.Equal(2, shipping.Calls);
        Assert.Equal(
            [99m, 198m],
            shipping.Requests
                .Select(request => request.FinalGroupGoodsAmountAfterDiscountAndTax!.Value)
                .OrderBy(amount => amount)
                .ToArray());
        Assert.Equal(
            [DraftA, DraftA],
            shipping.Requests.Select(request => request.DraftId).ToArray());
        Assert.All(shipping.Requests, request => Assert.Equal(3, request.SourceRevision));
        // Shared Store owner: active option + null-option probe once each (pointer cache).
        Assert.Equal(2, selected.Calls);

        var quotedGroups = result.Groups.Where(group => group.Methods.Count > 0).ToArray();
        Assert.Equal(2, quotedGroups.Length);
        Assert.Equal(["99.00", "198.00"], quotedGroups.Select(group => group.FinalGoodsAmountAfterDiscountAndTax).ToArray());

        var firstMethod = quotedGroups[0].Methods.Single();
        Assert.Equal("15000", firstMethod.FreightAmount);
        Assert.Equal("2000", firstMethod.PackagingAmount);
        Assert.Equal("IRR", firstMethod.Currency);
        // تعداد تاریخ‌ها دقیقاً از سیاست صریح AvailableDateCount (۷) می‌آید، نه از افق روش.
        Assert.Equal(7, firstMethod.AvailableDates.Count);
        Assert.Equal("09:00–12:00", firstMethod.AvailableDates[0].IntervalText);
        Assert.Equal("10:15–13:45", firstMethod.AvailableDates[1].IntervalText);
        Assert.True(firstMethod.AvailableDates[0].IsEarliest);

        var secondMethod = quotedGroups[1].Methods.Single();
        Assert.Equal("25000", secondMethod.FreightAmount);
        Assert.Equal("0", secondMethod.PackagingAmount);

        var unassignedGroup = Assert.Single(result.Groups.Where(group => group.Methods.Count == 0));
        Assert.Equal(StorefrontCapturedPreviewReasons.Unassigned, unassignedGroup.UnavailableReason);
        Assert.Equal([unassignedLine], unassignedGroup.CartLineIds);
        Assert.Null(result.NoticeReason);
    }

    /// <summary>دو Binding واقعی با برچسب ترجمه‌شدهٔ یکسان هر دو زنده می‌مانند (هویت از کد، نه برچسب).</summary>
    [Fact]
    public async Task Two_distinct_bindings_sharing_one_translated_label_both_survive()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(
            OfferA,
            harness.SellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        var catalog = new CatalogFake();
        catalog.Services = [Service("post", "standard", "express")];
        var selected = new SelectedCaptureFake
        {
            Respond = request => request.OptionCode is null
                ? Absent()
                : Selected(
                    request,
                    StoreTypeId,
                    request.OptionCode == "express" ? DraftB : DraftA,
                    revision: request.OptionCode == "express" ? 5 : 3,
                    label: "ارسال یکسان"),
        };
        var shipping = new RecordingCapturedQuoteLookup
        {
            // پاسخ هر نامزد همان Binding خودش را می‌گوید؛ وگرنه ۲۱۸ عمداً رد می‌کند.
            Respond = request => Quoted(
                request,
                10_000m,
                0m,
                week: null,
                optionCode: request.DraftId == DraftB ? "express" : "standard"),
        };
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var planner = Planner(
            true,
            assignments,
            catalog,
            ActiveTypeFake(),
            selected,
            composer: new StorefrontCapturedGroupProjectionComposer(
                harness.Directory,
                shipping,
                new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero))));

        var result = await planner.BuildAsync(
            CartFor(harness.CartId, Line(LineA, OfferA, harness.SellerId)),
            new CartAccess(harness.UserId, null),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        var group = Assert.Single(result.Groups);
        Assert.Equal(2, group.Methods.Count);
        Assert.Equal(["standard", "express"], group.Methods.Select(method => method.OptionCode).ToArray());
        Assert.All(group.Methods, method => Assert.Equal("ارسال یکسان", method.MethodLabel));
    }

    /// <summary>
    /// تفاوت بزرگ/کوچک حرف در سرویس همان تعهد است: یک خواندن ۲۱۹ و یک نامزد، نه دو.
    /// </summary>
    [Fact]
    public async Task Case_variant_service_binding_is_one_pointer_read_and_one_candidate()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(
            OfferA,
            harness.SellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        var catalog = new CatalogFake();
        catalog.Services = [Service("POST", "standard")];
        var selected = new SelectedCaptureFake
        {
            Respond = request => request.OptionCode is null
                ? Absent()
                : Selected(request, StoreTypeId, DraftA, revision: 3),
        };
        var shipping = new RecordingCapturedQuoteLookup
        {
            Respond = request => Quoted(request, 10_000m, 0m, week: null, serviceCode: "POST"),
        };
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var planner = Planner(
            true,
            assignments,
            catalog,
            ActiveTypeFake(),
            selected,
            composer: new StorefrontCapturedGroupProjectionComposer(
                harness.Directory,
                shipping,
                new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero))));

        var result = await planner.BuildAsync(
            CartFor(harness.CartId, Line(LineA, OfferA, harness.SellerId)),
            new CartAccess(harness.UserId, null),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        // Active option + null-option probe (null Absent) — one Selected candidate.
        Assert.Equal(2, selected.Calls);
        Assert.Single(Assert.Single(result.Groups).Methods);
    }

    /// <summary>
    /// TB-243-R1: post/NULL and post:standard coexist when both have Selected captures;
    /// active options no longer shadow the null-option publication identity.
    /// </summary>
    [Fact]
    public async Task Null_option_and_named_option_bindings_both_survive_when_selected()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(
            OfferA,
            harness.SellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        var catalog = new CatalogFake();
        catalog.Services = [Service("post", "standard")];
        var selected = new SelectedCaptureFake
        {
            Respond = request => Selected(
                request,
                StoreTypeId,
                request.OptionCode is null ? DraftA : DraftB,
                revision: request.OptionCode is null ? 2 : 4,
                label: request.OptionCode is null ? "پست بدون گزینه" : "پست استاندارد"),
        };
        var shipping = new RecordingCapturedQuoteLookup
        {
            Respond = request => Quoted(
                request,
                10_000m,
                0m,
                week: null,
                optionCode: request.DraftId == DraftA ? null : "standard"),
        };
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var planner = Planner(
            true,
            assignments,
            catalog,
            ActiveTypeFake(),
            selected,
            composer: new StorefrontCapturedGroupProjectionComposer(
                harness.Directory,
                shipping,
                new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero))));

        var result = await planner.BuildAsync(
            CartFor(harness.CartId, Line(LineA, OfferA, harness.SellerId)),
            new CartAccess(harness.UserId, null),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        var group = Assert.Single(result.Groups);
        Assert.Equal(2, group.Methods.Count);
        Assert.Contains(group.Methods, method => method.OptionCode is null);
        Assert.Contains(group.Methods, method => method.OptionCode == "standard");
        Assert.Equal(2, selected.Calls);
    }

    /// <summary>مقدار مسئولیت ناشناخته رد می‌شود و هرگز به Store/Platform پیش‌فرض نمی‌افتد.</summary>
    [Fact]
    public async Task Unknown_responsibility_value_is_refused_instead_of_default_store()
    {
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(
            OfferA,
            SellerA,
            (OfferFulfillmentResponsibility)99,
            StoreTypeId);
        var selected = new SelectedCaptureFake();
        var planner = Planner(true, assignments, new CatalogFake(), ActiveTypeFake(), selected);

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        var group = Assert.Single(result.Groups);
        Assert.Equal(StorefrontCapturedPreviewReasons.ResponsibilityInvalid, group.UnavailableReason);
        Assert.Empty(group.Methods);
        Assert.Equal(0, selected.Calls);
    }

    /// <summary>روشن‌بودن کلید بدون سیاست تعداد تاریخ معتبر هیچ خواندنی انجام نمی‌دهد.</summary>
    [Fact]
    public async Task Enabled_preview_without_a_valid_date_count_refuses_before_any_read()
    {
        var assignments = new AssignmentFake();
        var selected = new SelectedCaptureFake();
        var planner = new StorefrontCapturedPreviewPlanner(
            assignments,
            new CatalogFake(),
            new TypeFake(),
            selected,
            new StorefrontCapturedGroupProjectionComposer(
                new FixedGoodsFactsReader(null),
                new RecordingCapturedQuoteLookup(),
                new MutableClock(DateTimeOffset.UtcNow)),
            new EditionFake(ToobaEdition.SingleStore),
            new TenantFake("store-alpha"),
            Options.Create(new StorefrontCapturedPreviewOptions { Enabled = true, AvailableDateCount = null }));

        var result = await planner.BuildAsync(
            Cart(Line(LineA, OfferA, SellerA)),
            new CartAccess(null, "guest"),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None);

        Assert.True(result.Enabled);
        Assert.Equal(StorefrontCapturedPreviewReasons.AvailableDateCountInvalid, result.NoticeReason);
        Assert.Empty(result.Groups);
        Assert.Equal(0, assignments.Calls);
        Assert.Equal(0, selected.Calls);
    }

    /// <summary>خطای واقعی درز قیمت منتشر می‌شود و به «گروه‌های خالی/موفق» تبدیل نمی‌شود.</summary>
    [Fact]
    public async Task Dependency_failure_propagates_instead_of_empty_success()
    {
        using var harness = new ShippingGroupGoodsFactsHarness();
        var assignments = new AssignmentFake();
        assignments.Values[OfferA] = Assignment(
            OfferA,
            harness.SellerId,
            OfferFulfillmentResponsibility.Platform,
            StoreTypeId);
        var catalog = new CatalogFake();
        catalog.Services = [Service("post", "standard")];
        var selected = new SelectedCaptureFake
        {
            Respond = request => request.OptionCode is null
                ? Absent()
                : Selected(request, StoreTypeId, DraftA, revision: 3),
        };
        var shipping = new RecordingCapturedQuoteLookup { Failure = new InvalidOperationException("owner down") };
        harness.Carts.Cart = harness.Cart(harness.Line(LineA, OfferA, 100m));
        var planner = Planner(
            true,
            assignments,
            catalog,
            ActiveTypeFake(),
            selected,
            composer: new StorefrontCapturedGroupProjectionComposer(
                harness.Directory,
                shipping,
                new MutableClock(DateTimeOffset.UtcNow)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => planner.BuildAsync(
            CartFor(harness.CartId, Line(LineA, OfferA, harness.SellerId)),
            new CartAccess(harness.UserId, null),
            Destination,
            "IRR",
            LanguageId,
            CancellationToken.None));
    }

    /// <summary>کاتالوگ نوع با همان نوع واقعیِ فعال که انتساب‌ها به آن اشاره می‌کنند.</summary>
    private static TypeFake ActiveTypeFake()
    {
        var types = new TypeFake();
        types.Values[StoreTypeId] = Type(StoreTypeId, active: true);
        return types;
    }

    private static StorefrontCapturedPreviewPlanner Planner(
        bool enabled,
        AssignmentFake assignments,
        CatalogFake catalog,
        TypeFake types,
        SelectedCaptureFake selected,
        FixedGoodsFactsReader? goodsFacts = null,
        ToobaEdition edition = ToobaEdition.SingleStore,
        string? tenantId = "store-alpha",
        StorefrontCapturedGroupProjectionComposer? composer = null) =>
        new(
            assignments,
            catalog,
            types,
            selected,
            composer ?? new StorefrontCapturedGroupProjectionComposer(
                goodsFacts ?? new FixedGoodsFactsReader(null),
                new RecordingCapturedQuoteLookup(),
                new MutableClock(new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.Zero))),
            new EditionFake(edition),
            new TenantFake(tenantId),
            Options.Create(new StorefrontCapturedPreviewOptions { Enabled = enabled, AvailableDateCount = enabled ? 7 : null }));

    private static CartPage Cart(params CartLineView[] lines) => CartFor(Guid.NewGuid(), lines);

    /// <summary>سبد با همان شناسهٔ سبدی که Harness به Checkout می‌دهد (تفکیک هویت در ترکیب).</summary>
    private static CartPage CartFor(Guid cartId, params CartLineView[] lines) =>
        new(
            cartId,
            7,
            "IR",
            "IRR",
            "Online",
            lines.Length,
            [new CartCurrencyTotal("IRR", 100m)],
            lines,
            "guest",
            "Active");

    private static CartLineView Line(Guid lineId, Guid offerId, Guid sellerId) =>
        new(lineId, offerId, Guid.NewGuid(), sellerId, null, null, "کالا", "فروشنده", null, 1m, 100m, 100m, "IRR", false);

    private static OfferFulfillmentAssignmentSnapshot Assignment(
        Guid offerId,
        Guid sellerId,
        OfferFulfillmentResponsibility? responsibility,
        Guid? shippingTypeId) =>
        new(offerId, sellerId, responsibility, shippingTypeId);

    private static ShippingTypeSnapshot Type(Guid id, bool active) =>
        new(id, "standard", active, 10, 2, [new ShippingTypeTranslationSnapshot(LanguageId, "نوع", null)]);

    /// <summary>پاسخ کنترل‌شدهٔ مالک Shipping برای اثبات ترکیب مثبت (برچسب‌دار، نه provider واقعی).</summary>
    private static ShippingCapturedQuoteResult Quoted(
        ShippingCapturedQuoteLookupRequest request,
        decimal freight,
        decimal packaging,
        ShippingCapturedReceivingWeek? week,
        string serviceCode = "post",
        string? optionCode = "standard") =>
        new(
            ShippingCapturedQuoteVerdict.Quoted,
            ShippingCapturedQuoteVerdictCodes.Quoted,
            serviceCode,
            optionCode,
            "IRR",
            freight,
            packaging,
            freight + packaging,
            freight + packaging,
            0m,
            new ShippingCapturedQuoteSettings(
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual),
            SnapshotId: Guid.NewGuid(),
            DraftId: request.DraftId,
            SourceRevision: request.SourceRevision,
            FormatVersion: 1,
            ShippingTypeId: StoreTypeId,
            GroupId: Guid.NewGuid(),
            RateRowId: Guid.NewGuid(),
            MinDeliveryDays: 2,
            MaxDeliveryDays: 4,
            ReceivingWeek: week);

    /// <summary>هفتهٔ کاری کنترل‌شده (ISO ۱..۷) برای اثبات بازهٔ همان روز.</summary>
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

    private static ShippingCatalogServiceSnapshot Service(string code, params string[] options) =>
        new(
            Guid.NewGuid(),
            code,
            "Manual",
            "post",
            "primary",
            true,
            10,
            [new ShippingCatalogTranslationSnapshot(LanguageId, code, null)],
            options.Select(option => new ShippingCatalogOptionSnapshot(
                Guid.NewGuid(),
                Guid.Empty,
                option,
                true,
                10,
                [new ShippingCatalogOptionTranslationSnapshot(LanguageId, option)])).ToArray());

    private static ShippingSelectedCapturedRevisionResult Absent() =>
        new(
            ShippingSelectedCaptureVerdict.Absent,
            null,
            ShippingSelectedCaptureReadState.Absent,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            []);

    private static ShippingSelectedCapturedRevisionResult Selected(
        ShippingSelectedCapturedRevisionRequest request,
        Guid shippingTypeId,
        Guid draftId,
        long revision,
        string? label = null) =>
        new(
            ShippingSelectedCaptureVerdict.Selected,
            null,
            ShippingSelectedCaptureReadState.Selected,
            1,
            Guid.NewGuid(),
            draftId,
            revision,
            1,
            request.ServiceCode,
            request.OptionCode,
            shippingTypeId,
            [new ShippingSelectedCapturedTranslation(LanguageId, label ?? $"{request.ServiceCode} {request.OptionCode}", null)]);

    private sealed class AssignmentFake : IOfferFulfillmentAssignmentLookup
    {
        internal Dictionary<Guid, OfferFulfillmentAssignmentSnapshot> Values { get; } = [];
        internal int Calls { get; private set; }

        public Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken)
        {
            Calls++;
            IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot> result = Values
                .Where(pair => offerIds.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            return Task.FromResult(result);
        }
    }

    private sealed class CatalogFake : IShippingCatalogReader
    {
        internal IReadOnlyList<ShippingCatalogServiceSnapshot> Services { get; set; } = [];
        internal int Calls { get; private set; }

        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Services);
        }

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class TypeFake : IShippingTypeReader
    {
        internal Dictionary<Guid, ShippingTypeSnapshot> Values { get; } = [];

        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>(Values.Values.Where(type => type.IsActive).ToArray());

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>(
                Values.Values.Where(type => shippingTypeIds.Contains(type.ShippingTypeId)).ToArray());
    }

    private sealed class SelectedCaptureFake : IShippingSelectedCapturedRevisionLookup
    {
        private readonly List<ShippingSelectedCapturedRevisionRequest> _requests = [];

        internal Func<ShippingSelectedCapturedRevisionRequest, ShippingSelectedCapturedRevisionResult>? Respond { get; set; }

        internal IReadOnlyList<ShippingSelectedCapturedRevisionRequest> Requests => _requests;

        internal int Calls => _requests.Count;

        public Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
            ShippingSelectedCapturedRevisionRequest request,
            CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return Task.FromResult(
                Respond?.Invoke(request)
                ?? new ShippingSelectedCapturedRevisionResult(
                    ShippingSelectedCaptureVerdict.Absent,
                    null,
                    ShippingSelectedCaptureReadState.Absent,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    []));
        }
    }

    private sealed class EditionFake(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current { get; } = new(edition, "test");
    }

    private sealed class TenantFake(string? tenantId) : ICurrentTenant
    {
        public TenantContext? Current { get; } = tenantId is null
            ? null
            : new TenantContext(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("test"),
                null,
                null,
                null,
                "store.local",
                null);
    }
}


