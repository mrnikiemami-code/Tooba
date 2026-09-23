using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Application.Customer;
using Tooba.Order.Application.Storefront;

namespace Tooba.Order.Endpoints.Errors;

/// <summary>Explicit Order API error catalog (admin completeness + storefront + customer Order routes).</summary>
public sealed class OrderErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(AdminOrderCompletenessErrors.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Order was not found."),
        D(AdminOrderCompletenessErrors.InvalidNote, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Internal note body is required."),
        D(AdminOrderCompletenessErrors.DeleteForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "This internal note can no longer be deleted."),
        D(AdminOrderCompletenessErrors.InvoiceUnavailable, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Invoice is not available for this order."),
        D(AdminOrderCompletenessErrors.ReceiptUnavailable, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Payment receipt is not available for this order."),

        // Storefront checkout
        D(StorefrontOrderErrors.CheckoutMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "سفارش پیدا نشد."),
        D(StorefrontOrderErrors.CheckoutAccessDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید."),
        D(StorefrontOrderErrors.CheckoutAddressForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "این نشانی متعلق به مشتری جاری نیست."),
        D(StorefrontOrderErrors.CheckoutAuthenticationRequired, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "برای ادامه فرایند خرید وارد حساب خود شوید."),
        D(StorefrontOrderErrors.CheckoutOpenUnpaidLimit, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "شما به حداکثر تعداد سفارش‌های در انتظار پرداخت رسیده‌اید. ابتدا یکی از سفارش‌های قبلی را پرداخت یا لغو کنید."),
        D(StorefrontOrderErrors.CheckoutReservationCommitLimit, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "تعداد دفعات مجاز شروع رزرو در بازه زمانی اخیر به پایان رسیده است. کمی بعد دوباره تلاش کنید."),
        D(StorefrontOrderErrors.CheckoutInventoryUnavailable, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "موجودی یکی از کالاها برای ثبت سفارش کافی نیست."),
        D(StorefrontOrderErrors.CheckoutPriceChanged, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "قیمت یکی از کالاها تغییر کرده؛ لطفاً سفارش را دوباره بررسی کنید."),
        D(StorefrontOrderErrors.CheckoutTaxUnavailable, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "محاسبهٔ مالیات این سفارش الان ممکن نیست. لطفاً دوباره تلاش کنید."),
        D(StorefrontOrderErrors.CheckoutCartExpired, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "سبد خرید منقضی شده است."),
        D(StorefrontOrderErrors.CheckoutShippingIncomplete, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "اطلاعات ارسال کامل نیست."),
        D(StorefrontOrderErrors.CheckoutCartEmpty, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "سبد خرید خالی است."),
        D(StorefrontOrderErrors.CheckoutVersionConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "سبد هم‌زمان به‌روز شده است. صفحه را تازه کنید."),
        D(StorefrontOrderErrors.CheckoutRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "ثبت سفارش انجام نشد. لطفاً دوباره تلاش کنید."),
        D(StorefrontOrderErrors.CheckoutCartMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "سبد خرید پیدا نشد."),

        // Shipping
        D(StorefrontOrderErrors.ShippingAddressForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "نشانی انتخاب‌شده متعلق به این مشتری نیست."),
        D(StorefrontOrderErrors.ShippingCartMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "سبد خرید پیدا نشد."),
        D(StorefrontOrderErrors.ShippingCartEmpty, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "سبد خرید خالی است."),
        D(StorefrontOrderErrors.ShippingCartStale, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "سبد خرید تغییر کرده است؛ صفحه را تازه کنید."),
        D(StorefrontOrderErrors.ShippingCartForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "نشانی انتخاب‌شده متعلق به این مشتری نیست."),
        D(StorefrontOrderErrors.ShippingMethodUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "روش ارسال انتخاب‌شده در دسترس نیست."),
        D(StorefrontOrderErrors.ShippingDeliveryTooEarly, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "تاریخ تحویل نمی‌تواند زودتر از حداقل زمان آماده‌سازی باشد."),
        D(StorefrontOrderErrors.ShippingDeliverySlotUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "بازهٔ زمانی تحویل دیگر در دسترس نیست."),
        D(StorefrontOrderErrors.ShippingDeliveryInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "بازهٔ زمانی تحویل دیگر در دسترس نیست."),
        D(StorefrontOrderErrors.ShippingNoteTooLong, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "توضیحات سفارش بیش از حد طولانی است."),
        D(StorefrontOrderErrors.ShippingSelectionRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "ابتدا اطلاعات ارسال را تکمیل کنید."),
        D(StorefrontOrderErrors.ShippingFirstNameRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "نام الزامی است."),
        D(StorefrontOrderErrors.ShippingLastNameRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "نام خانوادگی الزامی است."),
        D(StorefrontOrderErrors.ShippingRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "امکان ادامهٔ مرحلهٔ ارسال وجود ندارد."),

        // Pending
        D(StorefrontOrderErrors.OrderCancelUnpaidOnly, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "لغو این سفارش از سبد فقط قبل از پرداخت موفق امکان‌پذیر است."),
        D(StorefrontOrderErrors.OrderCancelForbidden, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست."),
        D(StorefrontOrderErrors.PendingHideActiveHold, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "تا پایان مهلت رزرو نمی‌توان این کارت را پنهان کرد."),
        D(StorefrontOrderErrors.PaymentMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "پرداخت پیدا نشد."),
        D(StorefrontOrderErrors.PaymentRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "امکان انجام این عملیات در حال حاضر وجود ندارد."),

        // Customer panel Order
        D(CustomerOrderErrors.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Not Found"),
        D(CustomerOrderErrors.SessionRequired, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "Unauthorized"),
        D(CustomerOrderErrors.SupplyUnavailable, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "این سفارش در حال حاضر قابل تأمین نیست."),
        D(ReservationCycleErrors.RetryLimitReached, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            ReservationCycleErrors.RetryLimitReachedFa),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(
            Code: code,
            Classification: classification,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
