using Tooba.Cart.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;

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
    string RecipientLastName = "",
    IReadOnlyList<CheckoutShippingGroupSnapshot>? ShippingGroups = null);

/// <summary>گروه ارسال پایدار سفارش برای خواندن (M2).</summary>
public sealed record CheckoutShippingGroupSnapshot(
    Guid ShippingGroupId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string MethodLabel,
    decimal ShippingChargeSnapshot,
    string Currency,
    DateOnly MinimumDeliveryDate,
    DateOnly? RequestedDeliveryDate,
    string? RequestedDeliveryTimeWindow,
    int LeadDays,
    string State,
    IReadOnlyList<Guid> OrderLineIds);

/// <summary>
/// نتیجهٔ <b>فقط-خواندنی</b> «پیش‌نویس پرداخت» یک انتخاب capture‌شده (TB-234).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Quote"/> همان یک گذر مالی کل سبد است (یک نقل‌قول، بدون هیچ نوشتن) و
/// <see cref="Groups"/> همان گروه‌های <b>بازاعتبارسنجی‌شده</b> با هستهٔ پذیرفته‌شدهٔ ۲۳۳ (همان
/// اشاره‌گر/فهرست فعال/نشانی/تاریخ/مبلغ که ثبت نهایی می‌سنجد). هیچ سفارش/رزرو/تبدیلی ساخته نمی‌شود؛
/// دسترس‌پذیری تاریخ هم به‌معنای «قابل تسویه» یا فعال‌سازی نیست.
/// </para>
/// </remarks>
/// <param name="Quote">نقل‌قول فقط-خواندنی کل سبد.</param>
/// <param name="Groups">گروه‌های پذیرفته‌شدهٔ همین لحظه از انتخاب ذخیره‌شده.</param>
/// <param name="CartVersion">نسخهٔ <b>واقعی</b> سبدی که همین پذیرش بر آن اجرا شد.</param>
/// <param name="SelectionRevision">
/// نسخهٔ <b>واقعی</b> ریشهٔ انتخاب به‌شکل متن اعشاری دقیق (همان مقداری که کلاینت در CAS بعدی می‌فرستد)،
/// یا تهی وقتی انتخاب capture‌شده‌ای وجود ندارد. هیچ متن ساختگی/پیش‌فرضی ساخته نمی‌شود.
/// </param>
public sealed record CapturedPaymentDraftSnapshot(
    CheckoutSnapshot Quote,
    IReadOnlyList<OrderShippingGroupPlan> Groups,
    int CartVersion,
    string? SelectionRevision);

/// <summary>
/// TB-235-R1: انتقال تایپ‌شدهٔ <b>مالی/زمانیِ پذیرفته‌شده</b> به ترکیب snapshot پیش‌نمایش.
/// </summary>
/// <remarks>
/// پیش از این فقط مبلغ از پذیرش می‌آمد و تاریخ/روش از فرمان کهنه باقی می‌ماند؛ پس یک صفحه می‌توانست پول
/// ریشهٔ تازه و تاریخ/روش ریشهٔ کهنه را با هم نشان دهد. این مقدار کل تصویر ارسالِ snapshot را از
/// <b>یک</b> ریشهٔ پذیرفته‌شده می‌سازد و هیچ پنجرهٔ زمانیِ تجمیعی از گروه‌های مستقل نمی‌سازد: مقدار
/// مشترک فقط وقتی منتقل می‌شود که همهٔ گروه‌ها واقعاً همان را بگویند، وگرنه صریحاً تهی/خالی می‌ماند.
/// </remarks>
public sealed record CheckoutPreviewShippingOverride(
    decimal ShippingAmount,
    DateOnly? MinimumDeliveryDate,
    DateOnly? RequestedDeliveryDate,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string RequestedDeliveryTimeWindow)
{
    /// <summary>مقدار انتقال‌یافته از گروه‌های همین پذیرش؛ بدون گروه، تهی (فرمان تغییر نمی‌کند).</summary>
    public static CheckoutPreviewShippingOverride? Derive(IReadOnlyList<OrderShippingGroupPlan> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count == 0)
        {
            return null;
        }

        var codes = groups
            .Select(group => (group.MethodCode ?? string.Empty).Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var labels = groups
            .Select(group => (group.MethodLabel ?? string.Empty).Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var windows = groups
            .Select(group => (group.RequestedDeliveryTimeWindow ?? string.Empty).Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var requestedDates = groups
            .Select(group => group.RequestedDeliveryDate)
            .Distinct()
            .ToArray();

        return new CheckoutPreviewShippingOverride(
            groups.Sum(group => group.ShippingCharge),
            // زودترین تاریخ ممکنِ گروه‌ها یک مقدار واقعی است، نه یک پنجرهٔ ساختگی.
            groups.Min(group => group.MinimumDeliveryDate),
            requestedDates.Length == 1 ? requestedDates[0] : null,
            codes.Length == 1 ? codes[0] : string.Empty,
            labels.Length == 1 ? labels[0] : string.Empty,
            windows.Length == 1 ? windows[0] : string.Empty);
    }
}

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
    string RecipientLastName = "",
    IReadOnlyList<OrderShippingGroupPlan>? ShippingGroups = null,
    string? ExpectedSelectionRevision = null);
