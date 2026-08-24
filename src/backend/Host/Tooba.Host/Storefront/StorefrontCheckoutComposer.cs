using Tooba.Cart.Application;
using Tooba.Order.Application;
using Tooba.Order.Domain;

namespace Tooba.Host.Storefront;

/// <summary>
/// ترکیب نمایشی Checkout روی قرارداد Order. مبلغ نهایی در React ساخته نمی‌شود و سفارش Paid نمی‌شود.
/// </summary>
public sealed class StorefrontCheckoutComposer
{
    /// <summary>
    /// شناسهٔ عامل فروشگاهی مهمان. Party خریدار جدا است و در این برش هنوز ساخته نمی‌شود.
    /// </summary>
    public static readonly Guid StorefrontGuestActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000009");

    private const string DefaultShippingCode = "storefront-default";
    private const string DefaultShippingLabel = "ارسال پیش‌فرض فروشگاه";
    private const string TaxJurisdiction = "IR-NAT";

    private readonly StorefrontCartComposer _carts;
    private readonly ICheckoutDirectory _checkouts;

    /// <summary>
    /// سازندهٔ ترکیب checkout فروشگاه.
    /// </summary>
    public StorefrontCheckoutComposer(StorefrontCartComposer carts, ICheckoutDirectory checkouts)
    {
        _carts = carts;
        _checkouts = checkouts;
    }

    /// <summary>
    /// بازبینی تجاری سبد را بدون ساخت سفارش برمی‌گرداند.
    /// </summary>
    public async Task<StorefrontCheckoutPage> PreviewAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var quoted = await _checkouts.PreviewAsync(BuildCommand(cart, guestSecret, "preview"), cancellationToken);
        return MapPage(quoted, cart, persisted: false);
    }

    /// <summary>
    /// سبد را به CheckoutGroup و سفارش‌های PendingPayment تبدیل می‌کند.
    /// </summary>
    public async Task<StorefrontCheckoutPage> SubmitAsync(
        Guid cartId,
        string? guestSecret,
        int expectedVersion,
        string idempotencyKey,
        StorefrontCheckoutShippingInput shipping,
        CancellationToken cancellationToken)
    {
        ValidateShipping(shipping);
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        if (cart.Version != expectedVersion)
        {
            throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ checkout همزمان رد شد.");
        }

        var submitted = await _checkouts.SubmitAsync(
            BuildCommand(cart, guestSecret, idempotencyKey, shipping),
            cancellationToken);
        return MapPage(submitted, cart, persisted: true);
    }

    /// <summary>
    /// تأیید سفارش را پس از اثبات راز مهمان روی همان CartId برمی‌گرداند.
    /// </summary>
    public async Task<StorefrontCheckoutPage?> GetAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var snapshot = await _checkouts.GetCheckoutAsync(
            checkoutId,
            new OrderAccess(null, StorefrontGuestActorId),
            cancellationToken);
        if (snapshot is null || snapshot.CartId != cart.CartId)
        {
            return null;
        }

        return MapPage(snapshot, cart, persisted: true);
    }

    private async Task<StorefrontCartPage> RequireCartAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("سبد پیدا نشد.");
        if (cart.Lines.Count == 0)
        {
            throw new InvalidOperationException("سبد خالی به سفارش تبدیل نمی‌شود.");
        }

        return cart;
    }

    private static SubmitCheckoutCommand BuildCommand(
        StorefrontCartPage cart,
        string? guestSecret,
        string idempotencyKey,
        StorefrontCheckoutShippingInput? shipping = null) =>
        new(
            cart.CartId,
            new CartAccess(null, guestSecret),
            cart.Version,
            OrderMode.OnlinePurchase,
            null,
            StorefrontGuestActorId,
            idempotencyKey,
            TaxJurisdiction,
            null,
            null,
            shipping?.RecipientName ?? string.Empty,
            shipping?.ContactMobile ?? string.Empty,
            shipping?.ProvinceName ?? string.Empty,
            shipping?.CityName ?? string.Empty,
            shipping?.PostalAddress ?? string.Empty,
            shipping?.PostalCode ?? string.Empty,
            DefaultShippingCode,
            DefaultShippingLabel);

    private static StorefrontCheckoutPage MapPage(CheckoutSnapshot snapshot, StorefrontCartPage cart, bool persisted)
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

        return new StorefrontCheckoutPage(
            persisted ? snapshot.CheckoutId : null,
            snapshot.CartId,
            cart.Version,
            snapshot.Market,
            snapshot.Currency,
            snapshot.Channel.ToString(),
            "PendingPayment",
            string.IsNullOrWhiteSpace(snapshot.ShippingMethodCode) ? DefaultShippingCode : snapshot.ShippingMethodCode,
            string.IsNullOrWhiteSpace(snapshot.ShippingMethodLabel) ? DefaultShippingLabel : snapshot.ShippingMethodLabel,
            snapshot.RecipientName,
            snapshot.ContactMobile,
            snapshot.ProvinceName,
            snapshot.CityName,
            snapshot.PostalAddress,
            snapshot.PostalCode,
            sellers.Sum(x => x.SubtotalExclusiveOfTax),
            sellers.Sum(x => x.DiscountAmount),
            sellers.Sum(x => x.TaxAmount),
            0m,
            sellers.Sum(x => x.PayableAmount),
            sellers);
    }

    private static void ValidateShipping(StorefrontCheckoutShippingInput shipping)
    {
        if (string.IsNullOrWhiteSpace(shipping.RecipientName)
            || string.IsNullOrWhiteSpace(shipping.ContactMobile)
            || string.IsNullOrWhiteSpace(shipping.ProvinceName)
            || string.IsNullOrWhiteSpace(shipping.CityName)
            || string.IsNullOrWhiteSpace(shipping.PostalAddress)
            || string.IsNullOrWhiteSpace(shipping.PostalCode))
        {
            throw new InvalidOperationException("اطلاعات ارسال کامل نیست.");
        }
    }
}
