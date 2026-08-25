using Tooba.AddressBook.Application;
using Tooba.Cart.Application;
using Tooba.Order.Application;
using Tooba.Order.Domain;

namespace Tooba.Host.Storefront;

/// <summary>
/// ترکیب نمایشی Checkout روی قرارداد Order. مبلغ نهایی در React ساخته نمی‌شود و سفارش Paid نمی‌شود.
/// دفترچهٔ آدرس فقط برای تصویربرداری فیلدهای ارسال مصرف می‌شود و شناسهٔ نشانی روی سفارش ذخیره نمی‌گردد.
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
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    private readonly StorefrontCartComposer _carts;
    private readonly ICheckoutDirectory _checkouts;
    private readonly IAddressBookDirectory _addresses;
    private readonly CurrentAuthenticatedSession _session;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _http;

    /// <summary>
    /// سازندهٔ ترکیب checkout فروشگاه با درگاه دفترچه برای تصویربرداری اختیاری.
    /// داخلی است چون <see cref="CurrentAuthenticatedSession"/> عمومی نیست؛ ثبت DI با کارخانه در Program انجام می‌شود.
    /// </summary>
    internal StorefrontCheckoutComposer(
        StorefrontCartComposer carts,
        ICheckoutDirectory checkouts,
        IAddressBookDirectory addresses,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IHttpContextAccessor http)
    {
        _carts = carts;
        _checkouts = checkouts;
        _addresses = addresses;
        _session = session;
        _environment = environment;
        _http = http;
    }

    /// <summary>
    /// بازبینی تجاری سبد را بدون ساخت سفارش برمی‌گرداند.
    /// </summary>
    public async Task<StorefrontCheckoutPage> PreviewAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var quoted = await _checkouts.PreviewAsync(BuildCommand(cart, guestSecret, "preview", StorefrontGuestActorId), cancellationToken);
        return MapPage(quoted, cart, persisted: false);
    }

    /// <summary>
    /// سبد را به CheckoutGroup و سفارش‌های PendingPayment تبدیل می‌کند.
    /// در صورت وجود SavedAddressId، فیلدهای ارسال از دفترچهٔ متعلق به Actor تصویربرداری می‌شوند.
    /// </summary>
    public async Task<StorefrontCheckoutPage> SubmitAsync(
        Guid cartId,
        string? guestSecret,
        int expectedVersion,
        string idempotencyKey,
        StorefrontCheckoutShippingInput shipping,
        CancellationToken cancellationToken)
    {
        var prepared = await PrepareShippingAsync(shipping, cancellationToken);
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        if (cart.Version != expectedVersion)
        {
            throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ checkout همزمان رد شد.");
        }

        var submitted = await _checkouts.SubmitAsync(
            BuildCommand(cart, guestSecret, idempotencyKey, prepared.PlacedByUserId, prepared.Shipping),
            cancellationToken);
        return MapPage(submitted, cart, persisted: true);
    }

    /// <summary>
    /// تأیید سفارش را پس از اثبات راز مهمان روی همان CartId برمی‌گرداند.
    /// Actor خواندن با همان قاعدهٔ Submit Resolve می‌شود تا تأیید پس از ذخیرهٔ نشانی کار کند.
    /// </summary>
    public async Task<StorefrontCheckoutPage?> GetAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var actor = ResolvePlacementActor(usingSavedAddress: false);
        var snapshot = await _checkouts.GetCheckoutAsync(
            checkoutId,
            new OrderAccess(null, actor),
            cancellationToken);
        if (snapshot is null || snapshot.CartId != cart.CartId)
        {
            return null;
        }

        return MapPage(snapshot, cart, persisted: true);
    }

    /// <summary>
    /// فیلدهای ارسال را از دفترچه تصویربرداری می‌کند یا اعتبارسنجی درون‌خطی مهمان را نگه می‌دارد.
    /// شناسهٔ نشانی به سفارش منتقل نمی‌شود.
    /// </summary>
    internal async Task<StorefrontCheckoutPlacement> PrepareShippingAsync(
        StorefrontCheckoutShippingInput shipping,
        CancellationToken cancellationToken)
    {
        var usingSaved = shipping.SavedAddressId is Guid savedId && savedId != Guid.Empty;
        var actor = ResolvePlacementActor(usingSaved);
        if (!usingSaved)
        {
            ValidateShipping(shipping);
            return new StorefrontCheckoutPlacement(actor, shipping);
        }

        var saved = await _addresses.GetAsync(actor, shipping.SavedAddressId!.Value, cancellationToken);
        if (saved is null)
        {
            throw new InvalidOperationException("نشانی ذخیره‌شده متعلق به این مشتری نیست یا پیدا نشد.");
        }

        var snapshot = new StorefrontCheckoutShippingInput(
            saved.RecipientName,
            saved.ContactMobile,
            saved.ProvinceName ?? string.Empty,
            saved.CityName,
            saved.PostalAddress,
            saved.PostalCode);
        return new StorefrontCheckoutPlacement(actor, snapshot);
    }

    /// <summary>
    /// Actor ثبت سفارش را از نشست، سپس هدر Dev/Testing، و در غیر این صورت مهمان فروشگاه Resolve می‌کند.
    /// استفاده از دفترچه در Production بدون نشست رد می‌شود.
    /// </summary>
    internal Guid ResolvePlacementActor(bool usingSavedAddress)
    {
        if (_session.IsAuthenticated && _session.UserId is Guid userId && userId != Guid.Empty)
        {
            return userId;
        }

        var request = _http.HttpContext?.Request;
        var isDevSeam = _environment.IsDevelopment() || _environment.IsEnvironment("Testing");
        if (isDevSeam
            && request is not null
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var headerActor)
            && headerActor != Guid.Empty)
        {
            return headerActor;
        }

        if (usingSavedAddress && !isDevSeam)
        {
            throw new InvalidOperationException("برای استفاده از دفترچه آدرس نشست مشتری لازم است.");
        }

        return StorefrontGuestActorId;
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
        Guid placedByUserId,
        StorefrontCheckoutShippingInput? shipping = null) =>
        new(
            cart.CartId,
            new CartAccess(null, guestSecret),
            cart.Version,
            OrderMode.OnlinePurchase,
            null,
            placedByUserId,
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
            sellers.Count > 0 && sellers.All(x => string.Equals(x.Status, "Paid", StringComparison.Ordinal))
                ? "Paid"
                : "PendingPayment",
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

/// <summary>نتیجهٔ Resolve هویت ثبت و تصویر ارسال قبل از ماندگاری سفارش.</summary>
internal sealed record StorefrontCheckoutPlacement(Guid PlacedByUserId, StorefrontCheckoutShippingInput Shipping);
