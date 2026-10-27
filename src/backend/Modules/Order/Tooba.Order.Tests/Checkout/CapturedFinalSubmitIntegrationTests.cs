using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Tax.Contracts.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-233-R2: the ONE real positive captured submit through the ACTUAL
/// <see cref="CheckoutDirectory.SubmitAsync"/> path on the task-owned disposable PostgreSQL.
/// </summary>
/// <remarks>
/// Reservation/conversion are clearly labelled controlled Contracts (no live Inventory/Cart module is
/// contacted); Order persistence, <see cref="OrderDbContext"/>, migrations, the quote path and the
/// captured acceptance are real.
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class CapturedFinalSubmitIntegrationTests
{
    private static readonly Guid CartId = Guid.Parse("01a17d0d-e033-7000-af43-49a1daa28821");
    private static readonly Guid Buyer = Guid.Parse("01a17cf7-e38d-7000-82a5-00c57929bc54");
    private static readonly Guid SellerA = Guid.Parse("0199aaaa-0000-7000-8000-0000000000aa");
    private static readonly Guid SellerB = Guid.Parse("0199aaaa-0000-7000-8000-0000000000bb");
    private static readonly Guid OfferA = Guid.Parse("01a17cfc-0000-7000-8000-00000000000a");
    private static readonly Guid OfferB = Guid.Parse("01a17cfc-0000-7000-8000-00000000000b");
    private static readonly Guid LineA = Guid.Parse("01a17cfd-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("01a17cfd-0000-7000-8000-00000000000b");
    private static readonly Guid DraftGroupA = Guid.Parse("01a17d0f-0000-7000-8000-0000000000a1");
    private static readonly Guid DraftGroupB = Guid.Parse("01a17d0f-0000-7000-8000-0000000000b2");
    private static readonly Guid AreaA = Guid.Parse("01a17d10-0000-7000-8000-0000000000a1");
    private static readonly Guid AreaB = Guid.Parse("01a17d10-0000-7000-8000-0000000000b2");
    private static readonly Guid RowA = Guid.Parse("01a17d11-0000-7000-8000-0000000000a1");
    private static readonly Guid RowB = Guid.Parse("01a17d11-0000-7000-8000-0000000000b2");
    private static readonly Guid SnapshotId = Guid.Parse("01a17d0e-0419-7000-b500-0000000000a1");
    private static readonly Guid ShippingTypeId = Guid.Parse("01a1785e-01f3-7000-b500-0000000000c1");
    private static readonly Guid CountryId = Guid.Parse("e3451206-e503-51a8-9100-0000000000d1");
    private static readonly Guid ProvinceId = Guid.Parse("e3451206-e503-51a8-9100-0000000000d2");
    private static readonly Guid CityId = Guid.Parse("e3451206-e503-51a8-9100-0000000000d3");
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly ReceivingDate = new(2026, 10, 27);
    private static readonly long OpenTicks = new TimeOnly(9, 0).Ticks;
    private static readonly long CloseTicks = new TimeOnly(17, 0).Ticks;

    /// <summary>
    /// دو فروشنده روی <b>همان</b> Binding فروشگاهی: گروه‌های persist‌شدهٔ جدا، snapshot مشترک،
    /// منطقه/ردیف/تاریخ/مبلغ مستقل. مبلغ کالای نهایی ۹۹/۱۹۸ باید به همان دو فراخوان ۲۱۶ برسد.
    /// </summary>
    [Fact]
    public async Task Two_sellers_on_one_binding_submit_real_persisted_groups_and_evidence()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db);

        await SeedTwoGroupDraftAsync(db);
        var snapshot = await harness.Directory.SubmitAsync(
            harness.Command(expectedSelectionRevision: "1"),
            CancellationToken.None);

        Assert.Equal(CartId, snapshot.CartId);
        Assert.Equal(2, snapshot.SellerOrders.Count);

        db.ChangeTracker.Clear();
        var checkout = await db.Checkouts
            .Include(x => x.SellerOrders).ThenInclude(x => x.Lines)
            .Include(x => x.ShippingGroups).ThenInclude(x => x.Lines)
            .SingleAsync(x => x.CartId == CartId);

        var groups = checkout.ShippingGroups.ToArray();
        Assert.Equal(2, groups.Length);
        var groupA = Assert.Single(groups, x => x.ShippingGroupId == DraftGroupA);
        var groupB = Assert.Single(groups, x => x.ShippingGroupId == DraftGroupB);
        Assert.Equal(125m, groupA.ShippingChargeSnapshot);
        Assert.Equal(75m, groupB.ShippingChargeSnapshot);
        Assert.Single(groupA.Lines);
        Assert.Single(groupB.Lines);
        var orderLineIds = checkout.SellerOrders.SelectMany(x => x.Lines).Select(x => x.LineId).ToArray();
        Assert.Contains(groupA.Lines[0].OrderLineId, orderLineIds);
        Assert.Contains(groupB.Lines[0].OrderLineId, orderLineIds);

        var evidenceA = Assert.IsType<ShippingGroupAcceptedEvidence>(groupA.AcceptedEvidence);
        var evidenceB = Assert.IsType<ShippingGroupAcceptedEvidence>(groupB.AcceptedEvidence);
        Assert.Equal(ShippingGroupOwnerKind.Store, evidenceA.Owner.Kind);
        Assert.Equal("store-alpha", evidenceA.Owner.TenantKey);
        Assert.Equal("store-alpha", evidenceB.Owner.TenantKey);
        Assert.Equal(SnapshotId, evidenceA.SnapshotId);
        Assert.Equal(SnapshotId, evidenceB.SnapshotId);
        Assert.Equal(1L, evidenceA.SelectionVersion);
        Assert.Equal(ReceivingDate, evidenceA.ReceivingDate);
        Assert.Equal(OpenTicks, evidenceA.ReceivingOpenTicks);
        Assert.Equal(CloseTicks, evidenceA.ReceivingCloseTicks);
        Assert.Equal("UTC", evidenceA.ReceivingTimeZone);
        Assert.Equal(120m, evidenceA.FreightAmount);
        Assert.Equal(5m, evidenceA.PackagingAmount);
        Assert.Equal(0m, evidenceA.CarrierCollectAmount);
        Assert.Equal(70m, evidenceB.FreightAmount);

        // همان دو فراخوان ۲۱۶ با مبلغ کالای نهایی همان گروه؛ بدون Preview/217 دوم.
        Assert.Equal(2, harness.Quotes.Calls);
        Assert.Equal(
            new[] { 99m, 198m },
            harness.Quotes.Requests.Select(x => x.FinalGroupGoodsAmountAfterDiscountAndTax!.Value).OrderBy(x => x).ToArray());
        Assert.Equal(1, harness.AssignmentCalls);
        Assert.Equal(2, harness.PointerCalls);
        Assert.Equal(1, harness.Reservation.Calls);
        Assert.Equal(1, harness.Conversion.Calls);
        Assert.Equal(new[] { LineA, LineB }, harness.Reservation.LastLines.OrderBy(x => x).ToArray());
    }

    /// <summary>یک OfferId مشروع در دو خط سبد پذیرفته می‌شود (بدون هیچ استنتاج مقدار/مقدار دوم).</summary>
    [Fact]
    public async Task A_shared_offer_id_across_two_lines_is_lawful()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db, sharedOffer: true);

        await SeedSingleGroupDraftAsync(db, SellerA, [LineA, LineB]);
        var snapshot = await harness.Directory.SubmitAsync(
            harness.Command(expectedSelectionRevision: "1"),
            CancellationToken.None);

        var order = Assert.Single(snapshot.SellerOrders);
        Assert.Equal(2, order.Lines.Count);
    }

    /// <summary>ناسازگاری دروازه/مالک/اشاره‌گر/مبلغ/تاریخ/نسخه/کوپن پیش از هر رزرو/سفارش/تبدیل رد می‌شود.</summary>
    [Theory]
    [InlineData("gate")]
    [InlineData("owner")]
    [InlineData("pointer")]
    [InlineData("amount")]
    [InlineData("date")]
    [InlineData("coupon")]
    [InlineData("root")]
    public async Task Incoherent_captured_submit_is_refused_before_reservation(string scenario)
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(
            db,
            checkoutEnabled: scenario != "gate",
            freightA: scenario == "amount" ? 130m : null,
            quoteCloseHour: scenario == "date" ? 16 : 17,
            pointerRevision: scenario == "pointer" ? 3 : 2);

        await SeedTwoGroupDraftAsync(db, mutate: scenario);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.SubmitAsync(
                harness.Command(
                    expectedSelectionRevision: "1",
                    coupon: scenario == "coupon" ? "OFF10" : null),
                CancellationToken.None));

        Assert.False(string.IsNullOrWhiteSpace(error.Code));
        Assert.Equal(0, harness.Reservation.Calls);
        Assert.Equal(0, harness.Conversion.Calls);
        db.ChangeTracker.Clear();
        Assert.False(await db.Checkouts.AnyAsync(x => x.CartId == CartId));
    }

    /// <summary>تکرار همان ارسال پس از تغییر اشاره‌گر، همان سفارش و همان شاهد اصلی را برمی‌گرداند.</summary>
    [Fact]
    public async Task A_repeat_submit_returns_the_original_checkout_and_evidence_without_requoting()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        using var harness = new CapturedSubmitHarness(db);

        await SeedTwoGroupDraftAsync(db);
        var command = harness.Command(expectedSelectionRevision: "1");
        var first = await harness.Directory.SubmitAsync(command, CancellationToken.None);
        var quotesAfterFirst = harness.Quotes.Calls;
        var pointersAfterFirst = harness.PointerCalls;

        var second = await harness.Directory.SubmitAsync(command, CancellationToken.None);

        Assert.Equal(first.CheckoutId, second.CheckoutId);
        Assert.Equal(2, second.ShippingGroups.Count);
        Assert.Equal(first.ShippingGroups.Select(x => x.ShippingGroupId).Order().ToArray(), second.ShippingGroups.Select(x => x.ShippingGroupId).Order().ToArray());
        Assert.Equal(quotesAfterFirst, harness.Quotes.Calls);
        Assert.Equal(pointersAfterFirst, harness.PointerCalls);
        Assert.Equal(1, harness.Reservation.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.Checkouts.CountAsync(x => x.CartId == CartId));
    }

    /// <summary>شکست سد پس از نوشتن سفارش، هیچ گروه/شاهد فشرده‌ای را پایدار نمی‌کند.</summary>
    [Fact]
    public async Task A_barrier_failure_after_the_order_write_leaves_no_captured_evidence()
    {
        var target = OrderEvidenceRelationalTestTarget.FromEnvironment();
        await using var db = await ReadyAsync(target);
        var failure = new InvalidOperationException("tb233r2-barrier");
        using var harness = new CapturedSubmitHarness(db, barrier: new FailingBarrier(failure));

        await SeedTwoGroupDraftAsync(db);
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Directory.SubmitAsync(harness.Command(expectedSelectionRevision: "1"), CancellationToken.None));

        Assert.Same(failure, thrown);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Checkouts.CountAsync(x => x.CartId == CartId));
        Assert.Equal(0, await db.Set<OrderShippingGroup>().CountAsync());
    }

    private sealed class FailingBarrier(Exception failure) : ICheckoutCommitBarrier
    {
        public Task OnAfterReserveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task OnAfterOrderWriteAsync(CancellationToken cancellationToken) =>
            Task.FromException(failure);

        public Task OnAfterCartConvertedWriteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// DI واقعی scope: دایرکتوری، خوانندهٔ فکت و هستهٔ ترکیب باید بدون چرخه resolve شوند و همان نمونهٔ
    /// scoped دایرکتوری خوانندهٔ فکت باشد.
    /// </summary>
    [Fact]
    public void The_module_scope_resolves_the_directory_reader_and_core_without_a_cycle()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => new OrderDbContext(
            new DbContextOptionsBuilder<OrderDbContext>().Options));
        services.AddScoped<IOrderUseCaseGuard>(_ => new FakeGuard());
        services.AddScoped<ICartQueryGateway>(_ => new FakeCartGateway());
        services.AddScoped<IOfferLookupGateway>(_ => new FakeOfferLookup());
        services.AddScoped<IPriceLookupGateway>(_ => new FakePriceLookup());
        services.AddScoped<IOrderInventoryLifecyclePort>(_ => new NoCallInventoryLifecycle());
        services.AddScoped<ITaxCalculator>(_ => new FakeTaxCalculator());
        services.AddScoped<ICheckoutPromotionPort>(_ => new FakePromotion());
        services.AddScoped<ICatalogCheckoutLookup>(_ => new FakeCatalogCheckout());
        services.AddScoped<ISellerOrderCancelFulfillmentGate>(_ => new NoCallCancelGate());
        services.AddScoped<IClock>(_ => new FixedClock());
        services.AddScoped<IIdGenerator>(_ => new SequentialIds());
        services.AddScoped<ICheckoutInventoryReservationPort>(_ => new NoCallReservationPort());
        services.AddScoped<ICartConversionPort>(_ => new NoCallCartConversion());
        services.AddScoped<IShippingCapturedRevisionQuoteLookup>(_ => new RecordingCapturedQuoteLookup());
        services.AddScoped<ICheckoutDirectory, CheckoutDirectory>();
        services.AddScoped<ICheckoutShippingGroupGoodsFactsReader>(
            sp => (ICheckoutShippingGroupGoodsFactsReader)sp.GetRequiredService<ICheckoutDirectory>());
        services.AddScoped<StorefrontCapturedGroupProjectionComposer>();
        services.AddScoped<StorefrontCapturedGroupCompositionCore>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var scope = provider.CreateScope();
        var directory = scope.ServiceProvider.GetRequiredService<ICheckoutDirectory>();
        var reader = scope.ServiceProvider.GetRequiredService<ICheckoutShippingGroupGoodsFactsReader>();
        var composer = scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupProjectionComposer>();
        var core = scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupCompositionCore>();

        Assert.Same(directory, reader);
        Assert.NotNull(composer);
        Assert.NotNull(core);
        Assert.Same(core, scope.ServiceProvider.GetRequiredService<StorefrontCapturedGroupCompositionCore>());
    }

    private sealed class CapturedSubmitHarness : IDisposable
    {
        internal readonly RecordingCapturedQuoteLookup Quotes = new();
        internal readonly CountingReservationPort Reservation = new();
        internal readonly RecordingConversionPort Conversion = new();
        internal readonly CountingAssignmentLookup Assignments = new();
        internal readonly FakeCartGateway Carts = new();
        internal readonly FakeOfferLookup Offers = new();
        internal readonly FakePriceLookup Prices = new();
        internal readonly FakePromotion Promotions = new();
        internal readonly FakeTaxCalculator Taxes = new();
        internal readonly FakeCatalogCheckout Catalog = new();
        internal readonly FakeGuard Guard = new();
        internal readonly CheckoutDirectory Directory;
        private readonly FixedCapturedPointerLookup _pointers = new();
        private readonly FixedShippingTypeReader _types = new();
        private readonly FixedGeography _geography = new();

        internal CapturedSubmitHarness(
            OrderDbContext db,
            bool checkoutEnabled = true,
            bool sharedOffer = false,
            decimal? freightA = null,
            int quoteCloseHour = 17,
            long pointerRevision = 2,
            ICheckoutCommitBarrier? barrier = null)
        {
            CheckoutEnabled = checkoutEnabled;
            SharedOffer = sharedOffer;
            FreightA = freightA;
            QuoteCloseHour = quoteCloseHour;
            _pointers.Revision = pointerRevision;
            Registrations.RegisterAll(Prices);
            Registrations.RegisterAll(Offers);
            Taxes.Rate = 0m;
            Quotes.Respond = Quoted;
            Directory = new CheckoutDirectory(
                db,
                Guard,
                Carts,
                Offers,
                Prices,
                new NoCallInventoryLifecycle(),
                Taxes,
                Promotions,
                Catalog,
                new NoCallCancelGate(),
                new FixedClock(),
                new SequentialIds(),
                NullLogger<CheckoutProcessManager>.Instance,
                commitBarrier: barrier ?? new NullCheckoutCommitBarrier(),
                abuseGate: new NullCheckoutAbuseGate(),
                inventoryReservation: Reservation,
                cartConversion: Conversion,
                capturedPreview: Options.Create(new StorefrontCapturedPreviewOptions
                {
                    Enabled = true,
                    SelectionEnabled = true,
                    CheckoutEnabled = CheckoutEnabled,
                    AvailableDateCount = 7,
                }),
                offerAssignments: Assignments,
                shippingTypes: _types,
                selectedCaptures: _pointers,
                capturedComposition: new StorefrontCapturedGroupCompositionCore(Quotes, new FixedClock()),
                geography: _geography,
                edition: new FakeEdition(),
                tenant: new FakeTenant());
            ConfigureCart();
        }

        internal bool CheckoutEnabled { get; }

        internal bool SharedOffer { get; }

        internal decimal? FreightA { get; }

        internal int QuoteCloseHour { get; }

        internal int PointerCalls => _pointers.Calls;

        internal int AssignmentCalls => Assignments.Calls;

        internal void ConfigureCart()
        {
            Carts.Cart = SharedOffer
                ? CartOf([Line(LineA, OfferA, SellerA), Line(LineB, OfferA, SellerA)])
                : CartOf([Line(LineA, OfferA, SellerA), Line(LineB, OfferB, SellerB)]);
        }

        internal SubmitCheckoutCommand Command(string? expectedSelectionRevision, string? coupon = null) => new(
            CartId,
            new CartAccess(Buyer, null),
            7,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            PlacedByUserId: Buyer,
            IdempotencyKey: "tb233r2-positive",
            TaxJurisdiction: "IR-NAT",
            RecipientName: "گیرنده",
            ContactMobile: "09120000000",
            ProvinceName: "تهران",
            CityName: "تهران",
            PostalAddress: "نشانی",
            PostalCode: "1234567890",
            ShippingMethodCode: "post:standard",
            ShippingMethodLabel: "پست",
            ShippingAmount: 200m,
            MinimumDeliveryDate: new DateOnly(2026, 10, 26),
            RequestedDeliveryDate: ReceivingDate,
            RequestedDeliveryTimeWindow: "09:00-17:00",
            CouponCode: coupon,
            ShippingGroups: [],
            ExpectedSelectionRevision: expectedSelectionRevision);

        public void Dispose()
        {
        }

        private static CartSnapshot CartOf(CartLineSnapshot[] lines) => new(
            CartId,
            CartStatus.Active,
            CartAccessKind.Authenticated,
            Buyer,
            "IR",
            "IRR",
            SalesChannel.Direct,
            Now.AddHours(1),
            CartConversionIntent.OnlinePurchase,
            7,
            lines);

        private static CartLineSnapshot Line(Guid lineId, Guid offerId, Guid sellerPartyId) => new(
            lineId,
            offerId,
            VariantOf(offerId),
            sellerPartyId,
            1m,
            ReservationId: null,
            QuotedAmount: offerId == OfferA ? 99m : 198m,
            QuotedCurrency: "IRR",
            QuotedTaxExclusive: true,
            PriceId: PriceOf(offerId),
            QuotedAt: Now);

        private ShippingCapturedQuoteResult Quoted(ShippingCapturedQuoteLookupRequest request)
        {
            var isA = SharedOffer || request.FinalGroupGoodsAmountAfterDiscountAndTax == 99m;
            var freight = isA ? FreightA ?? 120m : 70m;
            return new ShippingCapturedQuoteResult(
                ShippingCapturedQuoteVerdict.Quoted,
                ShippingCapturedQuoteVerdictCodes.Quoted,
                isA ? "post" : "tipax",
                isA ? "standard" : "express",
                "IRR",
                freight,
                5m,
                freight + 5m,
                freight + 5m,
                0m,
                Settings: null,
                SnapshotId: SnapshotId,
                DraftId: request.DraftId,
                SourceRevision: request.SourceRevision,
                FormatVersion: 1,
                ShippingTypeId: ShippingTypeId,
                GroupId: isA ? AreaA : AreaB,
                RateRowId: isA ? RowA : RowB,
                MinDeliveryDays: 2,
                MaxDeliveryDays: 3,
                ReceivingWeek: Week(QuoteCloseHour));
        }

        private static ShippingCapturedReceivingWeek Week(int closeHour) => new(
            "UTC",
            Enumerable.Range(1, 7)
                .Select(iso => new ShippingCapturedReceivingWeekDay(
                    iso,
                    true,
                    new TimeOnly(9, 0),
                    new TimeOnly(closeHour, 0)))
                .ToArray());
    }

    private static async Task SeedTwoGroupDraftAsync(OrderDbContext db, string? mutate = null)
    {
        var draft = NewDraft();
        var groupA = DraftGroup(DraftGroupA, SellerA, "post", "standard", 125m);
        var groupB = DraftGroup(DraftGroupB, SellerB, "tipax", "express", 75m);
        groupA.AddWholeLine(LineA, SellerA);
        groupB.AddWholeLine(LineB, SellerB);

        groupA.ReplaceAcceptedEvidence(
            EvidenceFor(
                mutate == "owner" ? "other-tenant" : "store-alpha",
                groupA,
                120m,
                null,
                1L),
            Now);

        groupB.ReplaceAcceptedEvidence(EvidenceFor("store-alpha", groupB, 70m, null, 1L), Now);

        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        draft.AddShippingGroups([groupA, groupB], Now);
        if (mutate != "root")
        {
            draft.BumpSelectionRevision(Now);
        }

        db.ShippingDrafts.Add(draft);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task SeedSingleGroupDraftAsync(OrderDbContext db, Guid sellerId, Guid[] lines)
    {
        var draft = NewDraft();
        var group = DraftGroup(DraftGroupA, sellerId, "post", "standard", 125m);
        foreach (var line in lines)
        {
            group.AddWholeLine(line, sellerId);
        }

        group.ReplaceAcceptedEvidence(EvidenceFor("store-alpha", group, 120m, null, 1L), Now);
        draft.SetDestinationIdentity(CountryId, ProvinceId, CityId, null, Now);
        draft.AddShippingGroups([group], Now);
        draft.BumpSelectionRevision(Now);
        db.ShippingDrafts.Add(draft);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static CartShippingDraft NewDraft() => CartShippingDraft.Create(
        CartId,
        "hash",
        7,
        "گیرنده",
        "09120000000",
        "تهران",
        "تهران",
        "نشانی",
        "1234567890",
        null,
        "post:standard",
        "پست",
        200m,
        new DateOnly(2026, 10, 26),
        ReceivingDate,
        "09:00-17:00",
        null,
        Now);

    private static CartShippingDraftGroup DraftGroup(
        Guid groupId,
        Guid sellerId,
        string serviceCode,
        string optionCode,
        decimal payable) => CartShippingDraftGroup.Create(
        groupId,
        CartId,
        sellerId,
        $"{serviceCode}:{optionCode}",
        serviceCode,
        optionCode,
        "روش",
        null,
        payable,
        "IRR",
        new DateOnly(2026, 10, 26),
        ReceivingDate,
        "09:00-17:00",
        0,
        Now);

    private static ShippingGroupAcceptedEvidence EvidenceFor(
        string tenant,
        CartShippingDraftGroup group,
        decimal freight,
        DateOnly? date,
        long selectionVersion)
    {
        var isA = group.ShippingGroupId == DraftGroupA;
        return ShippingGroupAcceptedEvidence.Create(
            SnapshotId,
            CartId,
            2,
            1,
            selectionVersion,
            ShippingGroupOwner.Store(tenant),
            isA ? AreaA : AreaB,
            isA ? RowA : RowB,
            ShippingTypeId,
            freight,
            5m,
            "IRR",
            freight + 5m,
            0m,
            date ?? ReceivingDate,
            OpenTicks,
            CloseTicks,
            "UTC",
            2,
            3);
    }

    private static Guid VariantOf(Guid offerId)
    {
        var bytes = offerId.ToByteArray();
        bytes[15] ^= 0x5a;
        return new Guid(bytes);
    }

    private static Guid PriceOf(Guid offerId)
    {
        var bytes = offerId.ToByteArray();
        bytes[14] ^= 0x33;
        return new Guid(bytes);
    }

    private static async Task<OrderDbContext> ReadyAsync(OrderEvidenceRelationalTestTarget target)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            builder, target.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        var db = new OrderDbContext(builder.Options);
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"order\".order_shipping_group_lines;"
            + "DELETE FROM \"order\".order_shipping_groups;"
            + "DELETE FROM \"order\".checkout_processes;"+"DELETE FROM \"order\".order_lines;"
            + "DELETE FROM \"order\".seller_orders;"
            + "DELETE FROM \"order\".checkouts;"
            + "DELETE FROM \"order\".cart_shipping_group_lines;"
            + "DELETE FROM \"order\".cart_shipping_groups;"
            + "DELETE FROM \"order\".cart_shipping_drafts;");
        db.ChangeTracker.Clear();
        return db;
    }

    private sealed class CountingReservationPort : ICheckoutInventoryReservationPort
    {
        internal int Calls { get; private set; }

        internal IReadOnlyList<Guid> LastLines { get; private set; } = [];

        public Task<CheckoutInventoryReservationResult> ReserveForCheckoutAsync(
            CheckoutInventoryReservationRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastLines = request.Lines.Select(x => x.CartLineId).ToArray();
            return Task.FromResult(new CheckoutInventoryReservationResult(
                request.Lines
                    .Select((line, index) => new CheckoutInventoryLineReservation(
                        line.CartLineId,
                        Guid.Parse($"01a17d12-0000-7000-8000-{index:D12}")))
                    .ToArray()));
        }

        public Task ReleaseAsync(IEnumerable<Guid> reservationIds, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingConversionPort : ICartConversionPort
    {
        internal int Calls { get; private set; }

        public Task<CartConversionResult> ConvertForCheckoutAsync(
            CartConversionRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new CartConversionResult(
                request.CartId, request.ExpectedVersion + 1, CartStatus.Converted, request.Intent));
        }
    }

    private sealed class CountingAssignmentLookup : IOfferFulfillmentAssignmentLookup
    {
        internal int Calls { get; private set; }

        public Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>>(
                offerIds.ToDictionary(
                    id => id,
                    id => new OfferFulfillmentAssignmentSnapshot(
                        id,
                        id == OfferA ? SellerA : SellerB,
                        OfferFulfillmentResponsibility.Platform,
                        ShippingTypeId)));
        }
    }

    private sealed class FixedShippingTypeReader : IShippingTypeReader
    {
        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>([Active()]);

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>(
                shippingTypeIds.Contains(ShippingTypeId) ? [Active()] : []);

        private static ShippingTypeSnapshot Active() => new(
            ShippingTypeId, "heavy", true, 1, 1, []);
    }

    private sealed class FixedCapturedPointerLookup : IShippingSelectedCapturedRevisionLookup
    {
        internal int Calls { get; private set; }

        internal long Revision { get; set; } = 2;

        public Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
            ShippingSelectedCapturedRevisionRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new ShippingSelectedCapturedRevisionResult(
                ShippingSelectedCaptureVerdict.Selected,
                null,
                ShippingSelectedCaptureReadState.Selected,
                1,
                SnapshotId,
                CartId,
                Revision,
                1,
                request.ServiceCode,
                request.OptionCode,
                ShippingTypeId,
                []));
        }
    }

    private sealed class FixedGeography : IGeographyLookup
    {
        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance resolves the destination only");

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance resolves the destination only");

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance resolves the destination only");

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the captured final acceptance resolves the destination only");

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken) =>
            Task.FromResult<DestinationIdentifiers?>(identifiers);

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken) =>
            Task.FromResult(DestinationResolution.Resolved(
                new DestinationIdentifiers(CountryId, ProvinceId, CityId, null)));
    }

    private sealed class FakeEdition : ICurrentEdition
    {
        public EditionContext? Current { get; } = new(ToobaEdition.SingleStore, "tb233r2");
    }

    private sealed class FakeTenant : ICurrentTenant
    {
        public TenantContext? Current { get; } = new(
            new TenantId("store-alpha"),
            TenantStatus.Active,
            new ConnectionReference("tb233r2"),
            "Tooba",
            null,
            "IR",
            "127.0.0.1",
            null);
    }

    private static class Registrations
    {
        internal static void RegisterAll(FakePriceLookup prices)
        {
            prices.Register(new PriceQuote(PriceOf(OfferA), OfferA, "IR", SalesChannel.Direct, 99m, "IRR", true, true));
            prices.Register(new PriceQuote(PriceOf(OfferB), OfferB, "IR", SalesChannel.Direct, 198m, "IRR", true, true));
        }

        internal static void RegisterAll(FakeOfferLookup offers)
        {
            offers.Register(new OfferReference(OfferA, VariantOf(OfferA), SellerA, SalesChannel.Direct, OfferStatus.Active, null));
            offers.Register(new OfferReference(OfferB, VariantOf(OfferB), SellerB, SalesChannel.Direct, OfferStatus.Active, null));
        }
    }
}
