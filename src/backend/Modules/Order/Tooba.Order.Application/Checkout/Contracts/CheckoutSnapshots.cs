using Tooba.Cart.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;

namespace Tooba.Order.Application.Checkout.Contracts;

/// <summary>
/// خط سفارش برای خواندن. موجودیت EF نیست و قیمت جاری Pricing نیست.
/// </summary>
public sealed record OrderLineSnapshot(
    Guid LineId,
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    decimal Quantity,
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
    DateTimeOffset? PromotionAppliedAtSnapshot,
    Guid? UnitOfMeasureIdSnapshot = null,
    string? UnitCodeSnapshot = null,
    string? UnitDisplaySnapshot = null,
    int QuantityDecimalPlacesSnapshot = 0,
    decimal? QuantityStepSnapshot = null);

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
    string ShippingMethodLabel = "",
    decimal ShippingAmount = 0m,
    DateOnly? MinimumDeliveryDate = null,
    DateOnly? RequestedDeliveryDate = null,
    string RequestedDeliveryTimeWindow = "",
    string CustomerNote = "",
    string RecipientFirstName = "",
    string RecipientLastName = "");

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
    string ShippingMethodLabel = "",
    decimal ShippingAmount = 0m,
    DateOnly? MinimumDeliveryDate = null,
    DateOnly? RequestedDeliveryDate = null,
    string RequestedDeliveryTimeWindow = "",
    string CustomerNote = "",
    string RecipientFirstName = "",
    string RecipientLastName = "");
