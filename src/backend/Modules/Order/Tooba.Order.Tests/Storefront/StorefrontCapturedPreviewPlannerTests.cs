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
            7,
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
            7,
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
            7,
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
            7,
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
            7,
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
            7,
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
            7,
            CancellationToken.None);

        // دو Binding واقعی کاتالوگ ⇒ دقیقاً دو خواندن ۲۱۹ برای همان مالک (نه بیشتر، نه کمتر).
        Assert.Equal(2, selected.Calls);
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
        selected.Respond = request => Selected(
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
            7,
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
            7,
            CancellationToken.None);

        Assert.Equal(StorefrontCapturedPreviewReasons.NoSelectedCapture, Assert.Single(result.Groups).UnavailableReason);
        Assert.Equal(0, goods.Calls);
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
        string? tenantId = "store-alpha") =>
        new(
            assignments,
            catalog,
            types,
            selected,
            new StorefrontCapturedGroupProjectionComposer(
                goodsFacts ?? new FixedGoodsFactsReader(null),
                new RecordingCapturedQuoteLookup(),
                new MutableClock(new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.Zero))),
            new EditionFake(edition),
            new TenantFake(tenantId),
            Options.Create(new StorefrontCapturedPreviewOptions { Enabled = enabled }));

    private static CartPage Cart(params CartLineView[] lines) =>
        new(
            Guid.NewGuid(),
            3,
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

    private static ShippingSelectedCapturedRevisionResult Selected(
        ShippingSelectedCapturedRevisionRequest request,
        Guid shippingTypeId,
        Guid draftId,
        long revision) =>
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
            [new ShippingSelectedCapturedTranslation(LanguageId, $"{request.ServiceCode} {request.OptionCode}", null)]);

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


