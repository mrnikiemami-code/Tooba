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
/// <param name="OptionCode">
/// گزینهٔ روش ارسال Fulfillment در صورت وجود؛ تهی یعنی خدمت گزینه‌ای ندارد. این هویت گزینه
/// است، نه بخشی از کد خدمت، و برای قیمت‌گذاری گزینه‌ای مصرف می‌شود.
/// </param>
public sealed record StorefrontShippingMethodView(
    string MethodCode,
    string Label,
    string ServiceCode,
    string IconKey,
    decimal PriceAmount,
    int LeadDays,
    bool IsFree,
    string EstimationLabel,
    string? OptionCode = null);

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
    string LastName = "",
    IReadOnlyList<StorefrontShippingGroupView>? ShippingGroups = null);

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

/// <summary>
/// درخواست تصویر ارسال (سبد + مقصد اختیاری + روش انتخابی). <c>CityName</c> و <c>Destination</c> فقط
/// مشاهده‌ای‌اند: هر دو اختیاری و سازگار با مشتریان قدیمی که تنها استان می‌فرستند، و هیچ‌کدام قیمت،
/// روش انتخاب‌شده یا promise مشتری را تغییر نمی‌دهند.
/// </summary>
/// <param name="CartId">شناسهٔ سبد.</param>
/// <param name="ProvinceName">استان انتخاب‌شده (کد یا برچسب).</param>
/// <param name="MethodCode">روش انتخابی.</param>
/// <param name="Language">زبان نمایش.</param>
/// <param name="CityName">شهر انتخاب‌شده (نام یا کد) در صورت وجود.</param>
/// <param name="Destination">شناسه‌های پایدار Geography در صورت وجود؛ در نبود آن‌ها از متن استفاده می‌شود.</param>
public sealed record StorefrontShippingProjectionRequest(
    Guid CartId,
    string? ProvinceName = null,
    string? MethodCode = null,
    string? Language = null,
    string? CityName = null,
    Tooba.Geography.Contracts.Places.DestinationIdentifiers? Destination = null);

/// <summary>
/// انتخاب یک گروه ارسال از سمت کلاینت: تنها قصد انتخاب (روش متعارف + خطوط سبد).
/// هیچ مبلغ، برچسب، فروشنده یا ارزی از کلاینت پذیرفته نمی‌شود؛ همه از منابع authoritative می‌آید.
/// </summary>
/// <param name="ShippingMethodCode">کد روش متعارف (مثل tipax:standard).</param>
/// <param name="CartLineIds">خطوط سبد این گروه (تمام-خط؛ هر خط دقیقاً در یک گروه).</param>
/// <param name="ServiceCode">کد سرویس؛ در صورت ارسال باید با هویت authoritative بخواند.</param>
/// <param name="OptionCode">کد گزینه؛ در صورت ارسال باید با هویت authoritative بخواند.</param>
/// <param name="OriginContext">
/// مبدأ fulfilment. در این اسلایس مرجع مورداعتمادی وجود ندارد، پس فقط خالی مجاز است و
/// مقدار اعلامی fail-closed رد می‌شود (بدون جعل انبار).
/// </param>
public sealed record StorefrontShippingGroupSelection(
    string ShippingMethodCode,
    IReadOnlyList<Guid> CartLineIds,
    string? ServiceCode = null,
    string? OptionCode = null,
    string? OriginContext = null);

/// <summary>
/// نمای یک گروه ارسال در پیش‌نویس سبد. هویت/برچسب/مبلغ/ارز/زمان‌بندی همه از گروه ذخیره‌شده می‌آید.
/// </summary>
public sealed record StorefrontShippingGroupView(
    Guid ShippingGroupId,
    Guid SellerPartyId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string MethodLabel,
    decimal ShippingAmount,
    string Currency,
    string MinimumDeliveryDate,
    string? SelectedDeliveryDate,
    string? SelectedDeliveryTimeWindow,
    int LeadDays,
    int LineCount);

/// <summary>
/// ذخیرهٔ انتخاب ارسال. <see cref="ShippingGroups"/> افزایشی و اختیاری است: در نبود آن همان
/// انتخاب تک‌روشی گذشته به‌عنوان shorthand سازگار پذیرفته و به یک گروه کامل تبدیل می‌شود.
/// </summary>
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
    string LastName = "",
    IReadOnlyList<StorefrontShippingGroupSelection>? ShippingGroups = null);

/// <summary>ثبت نهایی ارسال و ساخت checkout برای پرداخت.</summary>
public sealed record StorefrontShippingCommitRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string IdempotencyKey,
    string? CouponCode = null);

/// <summary>
/// ورودی «کلیک نهایی پرداخت» فروشگاهی. مبلغ/شناسهٔ سفارش/رزرو/مهلت در بدنه نیست؛ CartId تنها از مسیر
/// می‌آید، هویت تنها از مرز اعتماد، و روش پرداخت انتخاب‌شده با <see cref="UseWallet"/> یا
/// <see cref="ProviderCode"/> اعلام می‌شود (UseWallet یا ProviderCode=wallet فقط وقتی موجودی کل مبلغ را
/// پوشش دهد مجاز است؛ mixed deferred). کلید idempotency باید پایدار و از سمت فراخوان باشد.
/// </summary>
public sealed record StorefrontStartPaymentBody(
    int ExpectedCartVersion,
    string IdempotencyKey,
    bool UseWallet = false,
    string? ProviderCode = null,
    string? CouponCode = null);

/// <summary>
/// پاسخ موفق «کلیک نهایی پرداخت»: مرجع پایدار سفارش و پرداخت. هیچ فیلد مشتق/حدسی اضافه نمی‌شود.
/// </summary>
public sealed record StorefrontStartPaymentResponse(
    Guid CheckoutId,
    bool CheckoutReplayed,
    Guid PaymentId,
    Guid AttemptId,
    string PaymentStatus,
    string ProviderCode,
    string RedirectUrl,
    decimal Amount,
    string Currency,
    bool RequiresPspRedirect,
    int ReservationCount,
    DateTimeOffset? GuaranteedUntilUtc);

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
