namespace Tooba.Order.Application.Storefront.Models;

/// <summary>
/// تصویر ارسال فروشگاهی. SavedAddressId اختیاری است و روی سفارش ذخیره نمی‌شود؛
/// فقط برای تصویربرداری فیلدها از دفترچهٔ متعلق به Actor استفاده می‌گردد.
/// </summary>
public sealed record StorefrontCheckoutShippingInput(
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    Guid? SavedAddressId = null,
    string FirstName = "",
    string LastName = "");

/// <summary>
/// ورودی ارسال checkout از سبد زنده.
/// </summary>
public sealed record StorefrontSubmitCheckoutRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string IdempotencyKey,
    StorefrontCheckoutShippingInput Shipping,
    string? CouponCode = null);

/// <summary>
/// خط بازبینی checkout. مبلغ از Order/Tax است نه جمع React.
/// </summary>
public sealed record StorefrontCheckoutLineView(
    Guid OfferId,
    Guid SellerPartyId,
    string Title,
    string SellerDisplayName,
    decimal Quantity,
    decimal LineExclusiveOfTax,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LinePayable,
    string Currency);

/// <summary>
/// سفارش فروشنده داخل checkout. پرداخت‌شده نیست.
/// </summary>
public sealed record StorefrontSellerOrderView(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    decimal SubtotalExclusiveOfTax,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<StorefrontCheckoutLineView> Lines);

/// <summary>
/// صفحهٔ checkout/تأیید. جمع نهایی از backend است.
/// </summary>
public sealed record StorefrontCheckoutPage(
    Guid? CheckoutId,
    Guid CartId,
    int CartVersion,
    string Market,
    string Currency,
    string Channel,
    string PaymentState,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string FirstName,
    string LastName,
    decimal SubtotalExclusiveOfTax,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal PayableAmount,
    IReadOnlyList<StorefrontSellerOrderView> SellerOrders,
    bool CanInitiatePayment = true);

/// <summary>روش ارسال قابل انتخاب فروشگاهی از کاتالوگ Store-enabled.</summary>
public sealed record StorefrontShippingMethodView(
    string MethodCode,
    string Label,
    string ServiceCode,
    string IconKey,
    decimal PriceAmount,
    int LeadDays,
    bool IsFree,
    string EstimationLabel);

/// <summary>گزینهٔ تاریخ تحویل.</summary>
public sealed record StorefrontDeliveryDateOption(
    string Value,
    string Label,
    string SubLabel,
    bool IsEarliest);

/// <summary>گزینهٔ پنجرهٔ ساعتی.</summary>
public sealed record StorefrontDeliveryTimeOption(string Value, string Label);

/// <summary>استان و شهرهای وابسته از کاتالوگ Host.</summary>
public sealed record StorefrontProvinceOption(string Code, string Label, IReadOnlyList<string> Cities);

/// <summary>پیش‌نویس ارسال ذخیره‌شده روی سبد.</summary>
public sealed record StorefrontShippingDraftView(
    Guid CartId,
    int CartVersion,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    Guid? SavedAddressId,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    decimal ShippingAmount,
    string MinimumDeliveryDate,
    string? SelectedDeliveryDate,
    string? SelectedDeliveryTimeWindow,
    string? CustomerNote,
    string FirstName = "",
    string LastName = "");

/// <summary>تصویر یکپارچهٔ مرحلهٔ ارسال.</summary>
public sealed record StorefrontShippingProjection(
    Guid CartId,
    int CartVersion,
    string Currency,
    decimal ItemCount,
    decimal SubtotalExclusiveOfTax,
    int SellerCount,
    int MaxSellerPreparationDays,
    IReadOnlyList<StorefrontShippingMethodView> Methods,
    string? SelectedMethodCode,
    decimal SelectedShippingAmount,
    string? MinimumDeliveryDate,
    IReadOnlyList<StorefrontDeliveryDateOption> DeliveryDates,
    IReadOnlyList<StorefrontDeliveryTimeOption> DeliveryTimeWindows,
    StorefrontShippingDraftView? Draft,
    string? RevalidationMessage,
    IReadOnlyList<StorefrontProvinceOption> Provinces);

/// <summary>درخواست تصویر ارسال (سبد + مقصد اختیاری + روش انتخابی).</summary>
public sealed record StorefrontShippingProjectionRequest(
    Guid CartId,
    string? ProvinceName = null,
    string? MethodCode = null,
    string? Language = null);

/// <summary>ذخیرهٔ انتخاب ارسال.</summary>
public sealed record StorefrontShippingSelectionRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    Guid? SavedAddressId,
    string ShippingMethodCode,
    string SelectedDeliveryDate,
    string SelectedDeliveryTimeWindow,
    string? CustomerNote,
    string FirstName = "",
    string LastName = "");

/// <summary>ثبت نهایی ارسال و ساخت checkout برای پرداخت.</summary>
public sealed record StorefrontShippingCommitRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string IdempotencyKey,
    string? CouponCode = null);

/// <summary>
/// ورودی شروع پرداخت فروشگاهی. مبلغ در بدنه نیست.
/// UseWallet یا ProviderCode=wallet فقط وقتی موجودی کل مبلغ را پوشش دهد مجاز است (mixed deferred).
/// </summary>

/// <summary>درخواست وابسته به سبد برای لغو/پنهان کارت.</summary>
public sealed record StorefrontPaymentCartRequest(Guid CartId);
public sealed record StorefrontPendingPaymentProofRequest(Guid CheckoutId, Guid CartId, string? GuestSecret);

/// <summary>درخواست تصویر دسته‌ای در انتظار پرداخت. مهمان فقط با proofs.</summary>
public sealed record StorefrontPendingPaymentQueryRequest(IReadOnlyList<StorefrontPendingPaymentProofRequest>? Proofs);

/// <summary>قلم فشرده برای کارت در انتظار پرداخت.</summary>
public sealed record StorefrontPendingPaymentLineView(string Title, decimal Quantity, Guid? MediaAssetId);

/// <summary>یک سفارش متعهد unpaid برای بخش در انتظار پرداخت.</summary>
public sealed record StorefrontPendingPaymentItemView(
    Guid CheckoutId,
    Guid CartId,
    string OrderReference,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<StorefrontPendingPaymentLineView> Items,
    string PaymentPresentation,
    bool CanInitiatePayment,
    bool CanRetryPayment,
    bool IsManualAwaitingReview,
    string PrimaryAction,
    string ReservationPresentation,
    int? CycleNumber,
    int SecondsRemaining,
    DateTimeOffset ServerTime,
    DateTimeOffset? HoldEndsAt,
    int MaxCycles,
    int RetryCountRemaining,
    string? SupplyStatus,
    Guid? PaymentId,
    bool HasReachedRetryLimit);

/// <summary>پاسخ دسته‌ای در انتظار پرداخت.</summary>
public sealed record StorefrontPendingPaymentPage(
    DateTimeOffset ServerTime,
    IReadOnlyList<StorefrontPendingPaymentItemView> Items);
