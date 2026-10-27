using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontPaymentDraft;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Validation;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-P0-CHECKOUT-PAYMENT-DRAFT-011 — «پیش‌نویس پرداخت» پیش از سفارش، آدرس‌دهی‌شده با CartId:
/// فقط خواندن؛ بدون CheckoutGroup/سفارش/رزرو/تبدیل سبد و بدون نوشتن پیش‌نویس ارسال.
/// سیم‌کشی فقط پورت‌های مسیر واقعی را fake می‌کند و بقیه null هستند تا اگر لمس شوند تست افشا کند.
/// </summary>
public sealed class StorefrontPaymentDraftTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-13T06:00:00Z", CultureInfo.InvariantCulture);
    private const string GuestSecret = "guest-secret-011";
    private const string MethodCode = "post";
    private const string MethodLabel = "پست";
    private const decimal ShippingAmount = 45_000m;
    private const decimal SellerSubtotal = 2_000_000m;
    private const decimal SellerTax = 180_000m;
    private const int CartVersion = 7;

    [Fact]
    public async Task Active_cart_with_saved_shipping_selection_yields_a_draft_without_any_checkout()
    {
        var harness = Harness.Create();
        var result = await new GetStorefrontPaymentDraftHandler(harness.Service).Handle(
            new GetStorefrontPaymentDraftQuery(harness.CartId, GuestSecret, CouponCode: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var page = result.Value;

        // هیچ سفارشی ساخته نشد: quote فقط در حافظه ارزیابی شد.
        Assert.Null(page.CheckoutId);
        Assert.Equal(harness.CartId, page.CartId);
        Assert.Equal(CartVersion, page.CartVersion);
        Assert.Equal("PendingPayment", page.PaymentState);
        Assert.True(page.CanInitiatePayment);

        // انتخاب ارسال ذخیره‌شده روی quote اعمال شده است (نه مقدار پیش‌فرض/ساختگی).
        Assert.Equal(MethodCode, page.ShippingMethodCode);
        Assert.Equal(MethodLabel, page.ShippingMethodLabel);
        Assert.Equal(ShippingAmount, page.ShippingAmount);
        Assert.Equal("تهران", page.ProvinceName);
        Assert.Equal("ایران‌شهر", page.CityName);

        // مبلغ‌ها از ارزیابی تجاری بک‌اند می‌آیند.
        Assert.Equal(SellerSubtotal, page.SubtotalExclusiveOfTax);
        Assert.Equal(SellerTax, page.TaxAmount);
        Assert.Equal(SellerSubtotal + SellerTax + ShippingAmount, page.PayableAmount);

        // فقط خواندن: یک quote، بدون Submit، بدون نوشتن پیش‌نویس.
        Assert.Equal(1, harness.Directory.PreviewCalls);
        Assert.Equal(0, harness.Directory.SubmitCalls);
        Assert.Equal(1, harness.Drafts.ReadCalls);
        Assert.Equal(0, harness.Drafts.SaveCalls);

        // کلید quote همان درز canonical preview است، پس مسیر رزرو/abuse اجرا نمی‌شود.
        Assert.Equal("preview", harness.Directory.LastCommand!.IdempotencyKey);
        Assert.Equal(OrderMode.OnlinePurchase, harness.Directory.LastCommand.Mode);

        // هویت مهمان تا CartAccess منتقل شده و کلید امنیتی جعل نشده است.
        Assert.Equal(GuestSecret, harness.Actor.LastGuestSecret);
        Assert.Equal(GuestSecret, harness.Directory.LastCommand.CartAccess.GuestSecret);
    }

    [Fact]
    public async Task Cart_without_a_saved_shipping_selection_fails_typed_and_never_quotes()
    {
        var harness = Harness.Create(withDraft: false);

        var exception = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.Service.ProjectPaymentDraftAsync(harness.CartId, GuestSecret, null, CancellationToken.None));

        Assert.Equal(StorefrontOrderErrors.ShippingSelectionRequired, exception.Code);
        Assert.Equal(0, harness.Directory.PreviewCalls);
        Assert.Equal(0, harness.Drafts.SaveCalls);
    }

    [Fact]
    public async Task Foreign_guest_secret_cannot_read_someone_elses_draft()
    {
        var harness = Harness.Create();

        var exception = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.Service.ProjectPaymentDraftAsync(harness.CartId, "wrong-secret", null, CancellationToken.None));

        Assert.Equal(StorefrontOrderErrors.ShippingCartForbidden, exception.Code);
        Assert.Equal(0, harness.Directory.PreviewCalls);
    }

    [Fact]
    public async Task Inaccessible_cart_is_not_addressable_and_its_draft_is_not_enumerable()
    {
        var harness = Harness.Create(cartAccessible: false);

        var exception = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.Service.ProjectPaymentDraftAsync(harness.CartId, GuestSecret, null, CancellationToken.None));

        // پاسخ یکسان برای سبد ناشناس/بیگانه، پیش از افشای وجود یا نبود پیش‌نویس.
        Assert.Equal(StorefrontOrderErrors.ShippingCartMissing, exception.Code);
        Assert.Equal(0, harness.Drafts.ReadCalls);
        Assert.Equal(0, harness.Directory.PreviewCalls);
    }

    [Fact]
    public async Task Query_transport_requires_a_cart_id()
    {
        var result = await new GetStorefrontPaymentDraftQueryValidator().ValidateAsync(
            new GetStorefrontPaymentDraftQuery(Guid.Empty, null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorCode == OrderValidationCodes.CartIdRequired);
    }

    private static string HashGuestSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim())));

    private static CartShippingDraft Draft(Guid cartId) =>
        CartShippingDraft.Create(
            cartId,
            HashGuestSecret(GuestSecret),
            CartVersion,
            "گیرنده تست",
            "09120000000",
            "تهران",
            "ایران‌شهر",
            "خیابان تست پلاک ۱",
            "1234567890",
            null,
            MethodCode,
            MethodLabel,
            ShippingAmount,
            new DateOnly(2026, 10, 16),
            new DateOnly(2026, 10, 17),
            "12-15",
            "یادداشت مشتری",
            Now);

    /// <summary>سیم‌کشی تست: فقط پورت‌های مسیر draft واقعی‌اند؛ بقیه null هستند تا اگر لمس شوند تست افشا کند.</summary>
    private sealed class Harness
    {
        public required Guid CartId { get; init; }
        public required StorefrontShippingService Service { get; init; }
        public required DirectoryFake Directory { get; init; }
        public required DraftStoreFake Drafts { get; init; }
        public required ActorFake Actor { get; init; }

        public static Harness Create(bool withDraft = true, bool cartAccessible = true)
        {
            var cartId = Guid.NewGuid();
            var directory = new DirectoryFake();
            var drafts = new DraftStoreFake(withDraft ? Draft(cartId) : null);
            var actor = new ActorFake();
            var carts = new CartsFake(cartId, cartAccessible);
            var checkout = new StorefrontCheckoutService(carts, directory, addresses: null!, actor);
            var service = new StorefrontShippingService(
                carts,
                checkout,
                addresses: null!,
                shippingCatalog: null!,
                oneLevelLabels: null!,
                drafts,
                languages: null!,
                shippingOptions: null!,
                shippingSeed: null!,
                actor,
                clock: new FixedClock(Now));
            return new Harness
            {
                CartId = cartId,
                Service = service,
                Directory = directory,
                Drafts = drafts,
                Actor = actor,
            };
        }
    }

    private sealed class CartsFake(Guid cartId, bool accessible) : ICartPresentationGateway
    {
        public Task<CartPage?> GetAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult(accessible && id == cartId ? Cart(id) : null);

        public Task<CartPage?> TryGetForOwnershipAsync(Guid id, string? guestSecret, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private static CartPage Cart(Guid id) =>
            new(
                id,
                CartVersion,
                "IR",
                "IRR",
                "Marketplace",
                2m,
                [new CartCurrencyTotal("IRR", SellerSubtotal)],
                [
                    new CartLineView(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        null,
                        null,
                        "کالای تست",
                        "فروشندهٔ تست",
                        null,
                        2m,
                        1_000_000m,
                        SellerSubtotal,
                        "IRR",
                        false),
                ],
                null,
                "Active");
    }

    private sealed class DraftStoreFake(CartShippingDraft? draft) : IStorefrontShippingDraftStore
    {
        public int ReadCalls { get; private set; }

        public int SaveCalls { get; private set; }

        public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken)
        {
            ReadCalls++;
            return Task.FromResult(draft is not null && draft.CartId == cartId ? draft : null);
        }

        public Task SaveAsync(CartShippingDraft value, bool isNew, CancellationToken cancellationToken)
        {
            SaveCalls++;
            throw new NotSupportedException();
        }

        public Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken) =>
            GetByCartIdAsync(cartId, cancellationToken);

        public Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
            CartShippingDraft value,
            IReadOnlyList<CartShippingDraftGroup> groups,
            bool isNew,
            long expectedSelectionRevision,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ActorFake : IOrderStorefrontActor
    {
        public string? LastGuestSecret { get; private set; }

        public Guid GuestActorId => Guid.Empty;

        public bool IsAuthenticated => false;

        public Guid? AuthenticatedUserId => null;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => throw new NotSupportedException();

        public Guid? TryResolveListActor() => throw new NotSupportedException();

        public CartAccess BuildCartAccess(string? guestSecret)
        {
            LastGuestSecret = guestSecret;
            return new CartAccess(null, guestSecret);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    /// <summary>هر mutation خارج از PreviewAsync باید تست را بشکند (بدون نوشتن Checkout/رزرو/تبدیل سبد).</summary>
    private sealed class DirectoryFake : ICheckoutDirectory
    {
        public int PreviewCalls { get; private set; }

        public int SubmitCalls { get; private set; }

        public SubmitCheckoutCommand? LastCommand { get; private set; }

        public Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
        {
            PreviewCalls++;
            LastCommand = command;
            return Task.FromResult(Snapshot(command));
        }

        public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
        {
            SubmitCalls++;
            throw new NotSupportedException();
        }

        public Task<CapturedPaymentDraftSnapshot> ProjectCapturedPaymentDraftAsync(
            SubmitCheckoutCommand command,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutSnapshot?> GetCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(string orderNumber, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RestoreCancelledCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
            Guid checkoutId,
            Guid viewerUserId,
            int take,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
            Guid checkoutId,
            Guid actorUserId,
            string body,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
            Guid checkoutId,
            Guid noteId,
            Guid actorUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordAdminViewAsync(Guid checkoutId, Guid viewerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private static CheckoutSnapshot Snapshot(SubmitCheckoutCommand command) =>
            new(
                Guid.NewGuid(),
                command.CartId,
                command.Mode,
                command.BuyerPartyId,
                command.PlacedByUserId,
                "IR",
                "IRR",
                SalesChannel.Marketplace,
                Now,
                [
                    new SellerOrderSnapshot(
                        Guid.NewGuid(),
                        "SO-PREVIEW",
                        Guid.NewGuid(),
                        SellerOrderStatus.Submitted,
                        SellerSubtotal,
                        SellerTax,
                        0m,
                        SellerSubtotal + SellerTax,
                        "IRR",
                        []),
                ],
                RecipientName: command.RecipientName,
                ContactMobile: command.ContactMobile,
                ProvinceName: command.ProvinceName,
                CityName: command.CityName,
                PostalAddress: command.PostalAddress,
                PostalCode: command.PostalCode,
                ShippingMethodCode: command.ShippingMethodCode,
                ShippingMethodLabel: command.ShippingMethodLabel,
                ShippingAmount: command.ShippingAmount,
                MinimumDeliveryDate: command.MinimumDeliveryDate,
                RequestedDeliveryDate: command.RequestedDeliveryDate,
                RequestedDeliveryTimeWindow: command.RequestedDeliveryTimeWindow,
                CustomerNote: command.CustomerNote);
    }
}
