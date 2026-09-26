using Tooba.AddressBook.Contracts.Customer;
using Tooba.Cart.Contracts;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Domain;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>
/// Order-owned storefront checkout composition (preview/submit/get).
/// </summary>
public sealed class StorefrontCheckoutService
{
    public static readonly Guid StorefrontGuestActorId = Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId;

    private const string DefaultShippingCode = "storefront-default";
    private const string DefaultShippingLabel = "ارسال پیش‌فرض فروشگاه";
    private const string TaxJurisdiction = "IR-NAT";

    private readonly ICartPresentationGateway _carts;
    private readonly ICheckoutDirectory _checkouts;
    private readonly IAddressBookCheckoutLookup _addresses;
    private readonly IOrderStorefrontActor _actor;

    public StorefrontCheckoutService(
        ICartPresentationGateway carts,
        ICheckoutDirectory checkouts,
        IAddressBookCheckoutLookup addresses,
        IOrderStorefrontActor actor)
    {
        _carts = carts;
        _checkouts = checkouts;
        _addresses = addresses;
        _actor = actor;
    }

    public async Task<StorefrontCheckoutPage> PreviewAsync(
        Guid cartId,
        string? guestSecret,
        string? couponCode,
        CancellationToken cancellationToken)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var quoted = await _checkouts.PreviewAsync(
            BuildCommand(cart, guestSecret, "preview", _actor.GuestActorId, shipping: null, couponCode),
            cancellationToken);
        return MapPage(quoted, cart, persisted: false);
    }

    public async Task<StorefrontCheckoutPage> SubmitAsync(
        Guid cartId,
        string? guestSecret,
        int expectedVersion,
        string idempotencyKey,
        StorefrontCheckoutShippingInput shipping,
        string? couponCode,
        CancellationToken cancellationToken,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        decimal? shippingAmount = null,
        DateOnly? minimumDeliveryDate = null,
        DateOnly? requestedDeliveryDate = null,
        string? requestedDeliveryTimeWindow = null,
        string? customerNote = null)
    {
        var prepared = await PrepareShippingAsync(shipping, cancellationToken);
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        if (cart.Version != expectedVersion)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutVersionConflict);
        }

        var submitted = await _checkouts.SubmitAsync(
            BuildCommand(
                cart,
                guestSecret,
                idempotencyKey,
                prepared.PlacedByUserId,
                prepared.Shipping,
                couponCode,
                shippingMethodCode,
                shippingMethodLabel,
                shippingAmount,
                minimumDeliveryDate,
                requestedDeliveryDate,
                requestedDeliveryTimeWindow,
                customerNote),
            cancellationToken);
        return MapPage(submitted, cart, persisted: true);
    }

    public async Task<StorefrontCheckoutPage?> GetAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var owned = await GetOwnedForPaymentResultAsync(checkoutId, guestSecret, cancellationToken);
        if (owned is not null)
        {
            return owned;
        }

        _ = cartId;
        var actor = _actor.ResolvePlacementActor(usingSavedAddress: false);
        var snapshot = await _checkouts.GetCheckoutAsync(
            checkoutId,
            new OrderAccess(null, actor),
            cancellationToken);
        if (snapshot is not null)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAccessDenied);
        }

        return null;
    }

    public async Task<StorefrontCheckoutPage?> GetOwnedForPaymentResultAsync(
        Guid checkoutId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var actor = _actor.ResolvePlacementActor(usingSavedAddress: false);
        var snapshot = await _checkouts.GetCheckoutAsync(
            checkoutId,
            new OrderAccess(null, actor),
            cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        if (_actor.IsAuthenticated && _actor.AuthenticatedUserId is Guid userId && userId != Guid.Empty)
        {
            return MapPage(snapshot, StubCartPage(snapshot), persisted: true);
        }

        if (string.IsNullOrWhiteSpace(guestSecret))
        {
            return null;
        }

        var committedCart = await _carts.TryGetForOwnershipAsync(snapshot.CartId, guestSecret, cancellationToken);
        if (committedCart is null)
        {
            return null;
        }

        return MapPage(snapshot, committedCart, persisted: true);
    }

    private static CartPage StubCartPage(CheckoutSnapshot snapshot) =>
        new(
            snapshot.CartId,
            0,
            snapshot.Market,
            snapshot.Currency,
            snapshot.Channel.ToString(),
            0,
            Array.Empty<CartCurrencyTotal>(),
            Array.Empty<CartLineView>(),
            null,
            "Converted");

    public async Task<StorefrontCheckoutPlacement> PrepareShippingAsync(
        StorefrontCheckoutShippingInput shipping,
        CancellationToken cancellationToken)
    {
        var usingSaved = shipping.SavedAddressId is Guid savedId && savedId != Guid.Empty;
        var actor = _actor.ResolvePlacementActor(usingSaved);
        if (!usingSaved)
        {
            ValidateShipping(shipping);
            return new StorefrontCheckoutPlacement(actor, shipping);
        }

        var saved = await _addresses.GetAsync(actor, shipping.SavedAddressId!.Value, cancellationToken);
        if (saved is null)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAddressForbidden);
        }

        var names = StorefrontRecipientNames.ResolveExplicitOverLegacy(
            shipping.FirstName,
            shipping.LastName,
            shipping.RecipientName,
            saved.FirstName,
            saved.LastName,
            saved.RecipientName);
        var snapshot = new StorefrontCheckoutShippingInput(
            names.Recipient,
            saved.ContactMobile,
            saved.ProvinceName ?? string.Empty,
            saved.CityName,
            saved.PostalAddress,
            saved.PostalCode,
            null,
            names.First,
            names.Last);
        return new StorefrontCheckoutPlacement(actor, snapshot);
    }

    private async Task<CartPage> RequireCartAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(cartId, guestSecret, cancellationToken)
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutCartMissing);
        if (cart.Lines.Count == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutCartEmpty);
        }

        // Order boundary: sole line currency is the only transaction currency; mixed/missing fails closed.
        _ = StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart);
        return cart;
    }

    private SubmitCheckoutCommand BuildCommand(
        CartPage cart,
        string? guestSecret,
        string idempotencyKey,
        Guid placedByUserId,
        StorefrontCheckoutShippingInput? shipping = null,
        string? couponCode = null,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        decimal? shippingAmount = null,
        DateOnly? minimumDeliveryDate = null,
        DateOnly? requestedDeliveryDate = null,
        string? requestedDeliveryTimeWindow = null,
        string? customerNote = null) =>
        new(
            cart.CartId,
            _actor.BuildCartAccess(guestSecret),
            cart.Version,
            OrderMode.OnlinePurchase,
            null,
            placedByUserId,
            idempotencyKey,
            TaxJurisdiction,
            string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim(),
            null,
            shipping?.RecipientName ?? string.Empty,
            shipping?.ContactMobile ?? string.Empty,
            shipping?.ProvinceName ?? string.Empty,
            shipping?.CityName ?? string.Empty,
            shipping?.PostalAddress ?? string.Empty,
            shipping?.PostalCode ?? string.Empty,
            string.IsNullOrWhiteSpace(shippingMethodCode) ? DefaultShippingCode : shippingMethodCode.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(shippingMethodLabel) ? DefaultShippingLabel : shippingMethodLabel.Trim(),
            shippingAmount ?? 0m,
            minimumDeliveryDate,
            requestedDeliveryDate,
            requestedDeliveryTimeWindow ?? string.Empty,
            customerNote ?? string.Empty,
            shipping?.FirstName ?? string.Empty,
            shipping?.LastName ?? string.Empty);

    private static StorefrontCheckoutPage MapPage(CheckoutSnapshot snapshot, CartPage cart, bool persisted)
    {
        var titleByOffer = cart.Lines.ToDictionary(x => x.OfferId, x => (x.Title, x.SellerDisplayName));
        var sellers = snapshot.SellerOrders.Select(order =>
        {
            var sellerName = order.Lines
                .Select(line => titleByOffer.GetValueOrDefault(line.OfferId).SellerDisplayName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                ?? "فروشنده";
            var lines = order.Lines.Select(line =>
            {
                var names = titleByOffer.GetValueOrDefault(line.OfferId);
                return new StorefrontCheckoutLineView(
                    line.OfferId,
                    line.SellerPartyId,
                    string.IsNullOrWhiteSpace(names.Title) ? "کالا" : names.Title,
                    string.IsNullOrWhiteSpace(names.SellerDisplayName) ? sellerName : names.SellerDisplayName,
                    line.Quantity,
                    line.LineTotalSnapshot,
                    line.DiscountAmountSnapshot,
                    line.TaxAmountSnapshot,
                    line.TaxInclusiveSnapshot,
                    line.Currency);
            }).ToList();
            return new StorefrontSellerOrderView(
                order.SellerOrderId,
                order.OrderNumber,
                order.SellerPartyId,
                sellerName,
                order.Status.ToString(),
                order.SubtotalSnapshot,
                order.TaxSnapshot,
                order.DiscountSnapshot,
                order.GrandTotalSnapshot,
                order.Currency,
                lines);
        }).ToList();

        var paymentState = sellers.Count > 0 && sellers.All(x => string.Equals(x.Status, "Paid", StringComparison.Ordinal))
            ? "Paid"
            : "PendingPayment";
        return new StorefrontCheckoutPage(
            persisted ? snapshot.CheckoutId : null,
            snapshot.CartId,
            cart.Version,
            snapshot.Market,
            snapshot.Currency,
            snapshot.Channel.ToString(),
            paymentState,
            string.IsNullOrWhiteSpace(snapshot.ShippingMethodCode) ? DefaultShippingCode : snapshot.ShippingMethodCode,
            string.IsNullOrWhiteSpace(snapshot.ShippingMethodLabel) ? DefaultShippingLabel : snapshot.ShippingMethodLabel,
            StorefrontRecipientNames.Display(snapshot.RecipientFirstName, snapshot.RecipientLastName, snapshot.RecipientName),
            snapshot.ContactMobile,
            snapshot.ProvinceName,
            snapshot.CityName,
            snapshot.PostalAddress,
            snapshot.PostalCode,
            snapshot.RecipientFirstName,
            snapshot.RecipientLastName,
            sellers.Sum(x => x.SubtotalExclusiveOfTax),
            sellers.Sum(x => x.DiscountAmount),
            sellers.Sum(x => x.TaxAmount),
            snapshot.ShippingAmount,
            sellers.Sum(x => x.PayableAmount) + snapshot.ShippingAmount,
            sellers,
            !string.Equals(paymentState, "Paid", StringComparison.Ordinal));
    }

    private static void ValidateShipping(StorefrontCheckoutShippingInput shipping)
    {
        if (string.IsNullOrWhiteSpace(shipping.ContactMobile)
            || string.IsNullOrWhiteSpace(shipping.ProvinceName)
            || string.IsNullOrWhiteSpace(shipping.CityName)
            || string.IsNullOrWhiteSpace(shipping.PostalAddress)
            || string.IsNullOrWhiteSpace(shipping.PostalCode))
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutShippingIncomplete);
        }

        var names = StorefrontRecipientNames.Resolve(shipping.FirstName, shipping.LastName, shipping.RecipientName);
        if (names.Recipient.Length == 0)
        {
            StorefrontRecipientNames.EnsureNewAddressNames(names.First, names.Last);
        }
    }
}

/// <summary>نتیجهٔ Resolve هویت ثبت و تصویر ارسال قبل از ماندگاری سفارش.</summary>
public sealed record StorefrontCheckoutPlacement(Guid PlacedByUserId, StorefrontCheckoutShippingInput Shipping);
