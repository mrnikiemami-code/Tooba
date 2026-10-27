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
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Tests.Checkout;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-229-R1: دروازهٔ نوشتن پیش‌نمایش capture‌شده — از طریق <b>همان متدهای سرویس</b>.
/// </summary>
/// <remarks>
/// <para>
/// وابستگی‌های سرویس عمداً <c>null</c> هستند: اگر دروازه اولین دستور نباشد و سبد/پیش‌نویس/checkout لمس
/// شود، به‌جای امتناع پایدار یک <see cref="NullReferenceException"/> می‌گیریم. پس رد شدن با کد صریح،
/// خودش اثبات «پیش از هر خواندن/نوشتن» است.
/// </para>
/// <para>
/// خاموش‌بودن کلید همان رفتار قبلی است: همان فراخوانی دیگر کد «آماده‌نیست» نمی‌دهد و مسیر قدیمی
/// دست‌نخورده می‌ماند (اینجا به‌خاطر وابستگی تهی با خطای دیگری متوقف می‌شود، نه با امتناع پیش‌نمایش).
/// </para>
/// </remarks>
public sealed class StorefrontCapturedPreviewWriteGuardTests
{
    private static readonly Guid CartId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Save_selection_is_refused_on_the_preview_code_before_any_dependency()
    {
        var service = Service(enabled: true);

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            service.SaveSelectionAsync(Selection(), null, CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    [Fact]
    public async Task Commit_is_refused_on_the_preview_code_before_any_dependency()
    {
        var service = Service(enabled: true);

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            service.CommitAsync(CartId, null, 7, "key", null, CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    [Fact]
    public async Task Disabled_option_keeps_the_existing_path_and_never_uses_the_preview_refusal()
    {
        var service = Service(enabled: false);

        var saveFailure = await Record.ExceptionAsync(() =>
            service.SaveSelectionAsync(Selection(), null, CancellationToken.None));
        var commitFailure = await Record.ExceptionAsync(() =>
            service.CommitAsync(CartId, null, 7, "key", null, CancellationToken.None));

        Assert.NotNull(saveFailure);
        Assert.NotNull(commitFailure);
        Assert.False(
            saveFailure is StorefrontOrderException saveError
            && saveError.Code == StorefrontCapturedPreviewReasons.PreviewNotReady);
        Assert.False(
            commitFailure is StorefrontOrderException commitError
            && commitError.Code == StorefrontCapturedPreviewReasons.PreviewNotReady);
    }

    [Fact]
    public async Task Commit_is_refused_for_a_stored_captured_draft_even_when_the_read_switch_is_off()
    {
        // TB-232: حتی با خاموش‌بودن خواندن/نوشتن، پیش‌نویسِ دارای شاهد capture هرگز به ثبت/پرداخت نمی‌رود.
        var service = Service(enabled: false, drafts: new CapturedDraftStore());

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            service.CommitAsync(CartId, null, 7, "key", null, CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    [Fact]
    public async Task Payment_draft_projection_is_refused_for_a_stored_captured_draft_even_when_the_switch_is_off()
    {
        // TB-232: خواندن صفحهٔ پرداخت هم پیش‌نویس capture‌شده را بازقیمت/آماده‌پرداخت نشان نمی‌دهد.
        // سبد واقعی داده می‌شود و composer پرداخت عمداً null است: رسیدن به مسیر قدیمی
        // NullReferenceException می‌داد، پس دریافت امتناع تایپ‌شده یعنی مسیر قدیمی لمس نشده است.
        var service = Service(enabled: false, drafts: new CapturedDraftStore(), carts: new OneLineCartGateway());

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            service.ProjectPaymentDraftAsync(CartId, null, null, CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    private sealed class OneLineCartGateway : ICartPresentationGateway
    {
        public Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult<CartPage?>(
                new CartPage(
                    cartId,
                    7,
                    "IR",
                    "IRR",
                    "Marketplace",
                    1m,
                    [new CartCurrencyTotal("IRR", 1_000_000m)],
                    [
                        new CartLineView(
                            Guid.Parse("01a17e00-0000-7000-9000-0000000000aa"),
                            Guid.Parse("01a17e00-0000-7000-9000-0000000000ab"),
                            Guid.Parse("01a17e00-0000-7000-9000-0000000000ac"),
                            Guid.Parse("01a17e00-0000-7000-9000-0000000000ad"),
                            null,
                            null,
                            "کالای تست",
                            "فروشندهٔ تست",
                            null,
                            1m,
                            1_000_000m,
                            1_000_000m,
                            "IRR",
                            false),
                    ],
                    null));

        public Task<CartPage?> TryGetForOwnershipAsync(
            Guid cartId,
            string? guestSecret,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private static StorefrontShippingService Service(bool enabled) =>
        new(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ShippingMethodsOptions(),
            null!,
            null!,
            new MutableClock(DateTimeOffset.UtcNow),
            capturedPreview: new StorefrontCapturedPreviewPlanner(
                new NoCallAssignments(),
                new NoCallCatalog(),
                new NoCallTypes(),
                new NoCallSelected(),
                new StorefrontCapturedGroupProjectionComposer(
                    new FixedGoodsFactsReader(null),
                    new RecordingCapturedQuoteLookup(),
                    new MutableClock(DateTimeOffset.UtcNow)),
                new NoCallEdition(),
                new NoCallTenant(),
                Options.Create(new StorefrontCapturedPreviewOptions
                {
                    Enabled = enabled,
                    AvailableDateCount = enabled ? 7 : null,
                })));

    private static StorefrontShippingService Service(
        bool enabled,
        IStorefrontShippingDraftStore drafts,
        IOrderStorefrontActor? actor = null,
        ICartPresentationGateway? carts = null) =>
        new(
            carts ?? null!,
            null!,
            null!,
            null!,
            null!,
            drafts,
            null!,
            new ShippingMethodsOptions(),
            null!,
            actor ?? new AnonymousActor(),
            new MutableClock(DateTimeOffset.UtcNow),
            capturedPreview: new StorefrontCapturedPreviewPlanner(
                new NoCallAssignments(),
                new NoCallCatalog(),
                new NoCallTypes(),
                new NoCallSelected(),
                new StorefrontCapturedGroupProjectionComposer(
                    new FixedGoodsFactsReader(null),
                    new RecordingCapturedQuoteLookup(),
                    new MutableClock(DateTimeOffset.UtcNow)),
                new NoCallEdition(),
                new NoCallTenant(),
                Options.Create(new StorefrontCapturedPreviewOptions
                {
                    Enabled = enabled,
                    AvailableDateCount = enabled ? 7 : null,
                })),
            capturedSelection: new StorefrontCapturedSelectionWriter(
                new StorefrontCapturedPreviewPlanner(
                    new NoCallAssignments(),
                    new NoCallCatalog(),
                    new NoCallTypes(),
                    new NoCallSelected(),
                    new StorefrontCapturedGroupProjectionComposer(
                        new FixedGoodsFactsReader(null),
                        new RecordingCapturedQuoteLookup(),
                        new MutableClock(DateTimeOffset.UtcNow)),
                    new NoCallEdition(),
                    new NoCallTenant(),
                    Options.Create(new StorefrontCapturedPreviewOptions
                    {
                        Enabled = enabled,
                        SelectionEnabled = enabled,
                        AvailableDateCount = enabled ? 7 : null,
                    })),
                drafts,
                null,
                new MutableClock(DateTimeOffset.UtcNow)));

    /// <summary>پیش‌نویس دارای شاهد ۲۳۱ — همان چیزی که نویسندهٔ اتمیک ذخیره می‌کند.</summary>
    private sealed class CapturedDraftStore : IStorefrontShippingDraftStore
    {
        private readonly CartShippingDraft _draft = CapturedDraft();

        public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
            Task.FromResult<CartShippingDraft?>(_draft.CartId == cartId ? _draft : null);

        public Task SaveAsync(CartShippingDraft value, bool isNew, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("a captured draft must never be written by the legacy path");

        public Task ReplaceCapturedSelectionAsync(
            CartShippingDraft draft,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("this fact only proves the refusal");

        private static CartShippingDraft CapturedDraft()
        {
            var now = new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.Zero);
            var date = new DateOnly(2026, 10, 29);
            var groupId = Guid.Parse("01a17e00-0000-7000-9000-0000000000a1");
            var groups = Domain.Checkout.CartShippingGroupPlanner.Plan(
                CartId,
                [new Domain.Checkout.CartShippingGroupCartLine(groupId, Guid.Parse("01a17e00-0000-7000-9000-0000000000aa"), 1m)],
                [
                    new Domain.Checkout.CartShippingGroupRequest(
                        "post",
                        "post",
                        "standard",
                        null,
                        [groupId],
                        date,
                        "09:00-12:00"),
                ],
                [
                    new Domain.Checkout.CartShippingGroupEligibleMethod(
                        "post",
                        "post",
                        "standard",
                        "پست",
                        17_000m,
                        "IRR",
                        2,
                        date,
                        Guid.Parse("01a17e00-0000-7000-9000-0000000000aa")),
                ],
                null,
                null,
                now,
                () => Guid.NewGuid());

            groups[0].ReplaceAcceptedEvidence(
                Domain.Checkout.ShippingGroupAcceptedEvidence.Create(
                    Guid.Parse("01a17e00-0000-7000-9000-0000000000e1"),
                    Guid.Parse("01a17e00-0000-7000-9000-0000000000d1"),
                    3,
                    1,
                    1,
                    Domain.Checkout.ShippingGroupOwner.Store("store-alpha"),
                    Guid.Parse("01a17e00-0000-7000-9000-0000000000f1"),
                    Guid.Parse("01a17e00-0000-7000-9000-0000000000f2"),
                    Guid.Parse("01a1785e-01f3-7000-b50d-d6372e756f89"),
                    15_000m,
                    2_000m,
                    "IRR",
                    17_000m,
                    0m,
                    date,
                    new TimeOnly(9, 0).Ticks,
                    new TimeOnly(17, 0).Ticks,
                    "Asia/Tehran",
                    2,
                    4),
                now);

            var draft = Domain.Checkout.CartShippingDraft.Create(
                CartId,
                null,
                7,
                "گیرنده",
                "09120000000",
                "تهران",
                "تهران",
                "خیابان",
                "1234567890",
                null,
                "post",
                "پست",
                17_000m,
                date,
                date,
                "09:00-12:00",
                null,
                now);
            draft.AddShippingGroups(groups, now);
            return draft;
        }
    }

    private sealed class AnonymousActor : IOrderStorefrontActor
    {
        public Guid GuestActorId => Guid.Empty;

        public bool IsAuthenticated => false;

        public Guid? AuthenticatedUserId => null;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => GuestActorId;

        public Guid? TryResolveListActor() => null;

        public CartAccess BuildCartAccess(string? guestSecret) => new(null, guestSecret);
    }

    private static StorefrontShippingSelectionRequest Selection() =>
        new(
            CartId,
            7,
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان",
            "1234567890",
            null,
            "post:standard",
            "2026-10-29",
            "morning",
            null);

    private sealed class NoCallAssignments : IOfferFulfillmentAssignmentLookup
    {
        public Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("assignments must not be read");
    }

    private sealed class NoCallCatalog : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("catalog must not be read");

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("catalog must not be read");
    }

    private sealed class NoCallTypes : IShippingTypeReader
    {
        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("types must not be read");

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("types must not be read");
    }

    private sealed class NoCallSelected : IShippingSelectedCapturedRevisionLookup
    {
        public Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
            ShippingSelectedCapturedRevisionRequest request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("selected capture must not be read");
    }

    private sealed class NoCallEdition : ICurrentEdition
    {
        public EditionContext? Current => throw new InvalidOperationException("edition must not be read");
    }

    private sealed class NoCallTenant : ICurrentTenant
    {
        public TenantContext? Current => throw new InvalidOperationException("tenant must not be read");
    }
}
