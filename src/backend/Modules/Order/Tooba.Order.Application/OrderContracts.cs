using Tooba.Cart.Application;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;

namespace Tooba.Order.Application;

/// <summary>
/// خط سفارش برای خواندن. موجودیت EF نیست و قیمت جاری Pricing نیست.
/// </summary>
public sealed record OrderLineSnapshot(
    Guid LineId,
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    int Quantity,
    decimal UnitPriceSnapshot,
    decimal LineTotalSnapshot,
    string Currency,
    bool TaxExclusive,
    Guid PriceId,
    Guid? ReservationId,
    string TaxOutcomeSnapshot,
    decimal TaxRateSnapshot,
    decimal TaxAmountSnapshot,
    decimal TaxInclusiveSnapshot,
    Guid? TaxRuleIdSnapshot,
    decimal DiscountAmountSnapshot,
    Guid? PromotionIdSnapshot,
    string? PromotionNameSnapshot,
    string? PromotionCodeSnapshot,
    string? DiscountKindSnapshot,
    decimal PreDiscountTaxExclusiveSnapshot,
    decimal PostDiscountTaxExclusiveSnapshot,
    DateTimeOffset? PromotionAppliedAtSnapshot);

/// <summary>
/// سفارش یک فروشنده داخل checkout. چرخهٔ ارسال نیست.
/// </summary>
public sealed record SellerOrderSnapshot(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    SellerOrderStatus Status,
    decimal SubtotalSnapshot,
    decimal TaxSnapshot,
    decimal DiscountSnapshot,
    decimal GrandTotalSnapshot,
    string Currency,
    IReadOnlyList<OrderLineSnapshot> Lines);

/// <summary>
/// نتیجهٔ checkout. سبد نیست و پرداخت انجام‌شده نیست.
/// </summary>
public sealed record CheckoutSnapshot(
    Guid CheckoutId,
    Guid CartId,
    OrderMode Mode,
    Guid? BuyerPartyId,
    Guid PlacedByUserId,
    string Market,
    string Currency,
    SalesChannel Channel,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<SellerOrderSnapshot> SellerOrders,
    string RecipientName = "",
    string ContactMobile = "",
    string ProvinceName = "",
    string CityName = "",
    string PostalAddress = "",
    string PostalCode = "",
    string ShippingMethodCode = "",
    string ShippingMethodLabel = "");

/// <summary>
/// فرمان ارسال checkout از روی سبد فعال.
/// </summary>
public sealed record SubmitCheckoutCommand(
    Guid CartId,
    CartAccess CartAccess,
    int ExpectedCartVersion,
    OrderMode Mode,
    Guid? BuyerPartyId,
    Guid PlacedByUserId,
    string IdempotencyKey,
    string TaxJurisdiction,
    string? CouponCode = null,
    decimal? QuotedDiscountAmount = null,
    string RecipientName = "",
    string ContactMobile = "",
    string ProvinceName = "",
    string CityName = "",
    string PostalAddress = "",
    string PostalCode = "",
    string ShippingMethodCode = "",
    string ShippingMethodLabel = "");

/// <summary>
/// هویت مجاز برای خواندن سفارش. شمارهٔ سفارش به‌تنهایی Bearer نیست.
/// </summary>
public sealed record OrderAccess(Guid? BuyerPartyId, Guid? PlacedByUserId);

/// <summary>
/// درز نگهبان مجوز Order. ماتریس نهایی فروشنده اینجا نیست.
/// </summary>
public interface IOrderUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن checkout را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// ارکستراسیون checkout روی قراردادهای Cart/Offer/Pricing/Inventory. DbContext آن‌ها لمس نمی‌شود.
/// </summary>
public interface ICheckoutDirectory
{
    /// <summary>
    /// سبد را به گروه checkout و سفارش‌های فروشنده تبدیل می‌کند. تکرار با همان کلید سفارش تکراری نمی‌سازد.
    /// </summary>
    Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// همان ارزیابی تجاری Submit را بدون ماندگاری CheckoutGroup برمی‌گرداند تا ویترین مبلغ را از React حساب نکند.
    /// </summary>
    Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// checkout را پس از احراز هویت خریدار یا کاربر عامل برمی‌گرداند.
    /// </summary>
    Task<CheckoutSnapshot?> GetCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken);

    /// <summary>
    /// سفارش فروشنده را با شمارهٔ مرجع پس از احراز هویت برمی‌گرداند. شماره به‌تنهایی کافی نیست.
    /// </summary>
    Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(string orderNumber, OrderAccess access, CancellationToken cancellationToken);

    /// <summary>
    /// سفارش فروشنده را در صورت ایمن بودن لغو می‌کند و رزرو را از قرارداد Inventory آزاد می‌کند.
    /// </summary>
    Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken);
}

/// <summary>اثبات خرید پرداخت‌شده که فقط از دادهٔ مالک Order ساخته می‌شود.</summary>
public sealed record OrderPurchaseVerification(bool IsVerified, Guid? SellerOrderId)
{
    /// <summary>نتیجهٔ بسته و بدون اثبات.</summary>
    public static OrderPurchaseVerification NotVerified { get; } = new(false, null);
}

/// <summary>درز Order-owned برای اثبات خرید محصول بدون وابستگی Order به Catalog.</summary>
public interface IOrderPurchaseVerificationGateway
{
    /// <summary>
    /// وجود سفارش Paid متعلق به Actor را برای یکی از شناسه‌های گونهٔ داده‌شده بررسی می‌کند؛
    /// نگاشت محصول به گونه‌ها پیش از فراخوانی و توسط مصرف‌کنندهٔ Catalog انجام می‌شود.
    /// </summary>
    Task<OrderPurchaseVerification> VerifyPaidPurchaseAsync(
        Guid actorUserId,
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);
}

/// <summary>
/// snapshot immutable سفارش برای handoff fulfillment.
/// </summary>
public sealed record OrderFulfillmentHandoffSnapshot(
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    Guid PlacedByUserId,
    bool IsPaid,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    IReadOnlyList<OrderFulfillmentLineSnapshot> Lines);

/// <summary>
/// خط سفارش برای fulfillment.
/// </summary>
public sealed record OrderFulfillmentLineSnapshot(
    Guid OrderLineId,
    int Quantity,
    Guid? ReservationId);

/// <summary>
/// خواندن snapshot سفارش برای Fulfillment بدون cross-DbContext.
/// </summary>
public interface IOrderFulfillmentReader
{
    /// <summary>
    /// snapshot handoff را برای SellerOrder برمی‌گرداند.
    /// </summary>
    Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(Guid sellerOrderId, CancellationToken cancellationToken);

    /// <summary>
    /// snapshot checkout را برای مشتری برمی‌گرداند.
    /// </summary>
    Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
