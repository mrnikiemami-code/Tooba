namespace Tooba.Host.Admin;

/// <summary>
/// شمارنده‌های عملیاتی داشبورد مدیر که فقط از دادهٔ واقعی ماژول‌ها ساخته می‌شوند.
/// </summary>
public sealed record AdminDashboardSummary(
    int PublishedProducts,
    int ActiveOffers,
    int OpenOrders,
    int PaidOrders,
    int PendingOrders,
    int Sellers,
    int Customers);

/// <summary>
/// ردیف سفارش تجمیعی مدیر بر پایهٔ Checkout و snapshotهای سفارش.
/// </summary>
public sealed record AdminOrderListItem(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    string CustomerDisplayName,
    int SellerCount,
    string SellerDisplayNames,
    int LineCount,
    decimal PayableAmount,
    string Currency,
    string PaymentState,
    string Status);

/// <summary>
/// خط سفارش مدیر؛ مبلغ از snapshot سفارش می‌آید و قیمت جاری Product نیست.
/// </summary>
public sealed record AdminOrderLineView(
    Guid OfferId,
    string ProductTitle,
    int Quantity,
    decimal UnitAmount,
    decimal LinePayable,
    string Currency);

/// <summary>
/// برش سفارش یک فروشنده در جزئیات Checkout مدیر.
/// </summary>
public sealed record AdminSellerOrderView(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    string PaymentState,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<AdminOrderLineView> Lines);

/// <summary>
/// برش مالی یک فروشنده در جزئیات سفارش مدیر.
/// </summary>
public sealed record AdminSellerFinancialView(
    Guid SellerOrderId,
    Guid SellerPartyId,
    string SellerDisplayName,
    int LineCount,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal PayableAmount,
    string Currency,
    string SettlementStatus);

/// <summary>
/// رویداد مالی در تاریخچهٔ checkout مدیر.
/// </summary>
public sealed record AdminFinancialEventView(
    DateTimeOffset OccurredAt,
    string EventType,
    decimal Amount,
    string Currency,
    string PartyDisplayName,
    string Reference,
    string PaymentMethod,
    string Status,
    string Description);

/// <summary>
/// جمع‌بندی مالی checkout برای تب خلاصهٔ مالی.
/// </summary>
public sealed record AdminFinancialSummaryView(
    decimal TotalSellerShare,
    decimal TotalCommission,
    decimal GrossOrderProfit,
    decimal PayableToSellers,
    decimal CustomerGrossAmount,
    decimal ShippingCost,
    decimal CustomerDiscounts,
    decimal TotalReceivedFromCustomer,
    string Currency);

/// <summary>
/// جزئیات عملیاتی سفارش با snapshot گیرنده و ارسال؛ راز پرداختی در آن وجود ندارد.
/// </summary>
public sealed record AdminOrderDetailPage(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    string Status,
    string PaymentState,
    int LineCount,
    int SellerCount,
    decimal Subtotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Currency,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string ShippingMethodLabel,
    IReadOnlyList<AdminSellerOrderView> SellerOrders,
    IReadOnlyList<AdminSellerFinancialView> SellerFinancials,
    IReadOnlyList<AdminFinancialEventView> FinancialEvents,
    AdminFinancialSummaryView FinancialSummary,
    AdminPaymentOpsView? Payment = null);

/// <summary>
/// ردیف دریافت مشتری (پرداخت) برای گرید Admin.
/// </summary>
public sealed record AdminReceiptListItem(
    Guid PaymentId,
    Guid CheckoutId,
    string OrderReference,
    string CustomerDisplayName,
    decimal Amount,
    string Currency,
    string Status,
    string ProviderCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>
/// ماندهٔ تسویه با نام نمایشی فروشنده برای گرید Admin.
/// </summary>
public sealed record AdminSettlementBalanceListItem(
    Guid SettlementAccountId,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Currency,
    decimal PostedCredits,
    decimal PostedDebits,
    decimal ReservedPayouts,
    decimal AvailableBalance);

/// <summary>
/// درخواست payout با نام نمایشی فروشنده برای گرید Admin.
/// </summary>
public sealed record AdminPayoutListItem(
    Guid PayoutRequestId,
    Guid SettlementAccountId,
    Guid SellerPartyId,
    string SellerDisplayName,
    decimal Amount,
    string Currency,
    string Status,
    string IdempotencyKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// بازرسی عملیاتی پرداخت روی جزئیات سفارش مدیر؛ راز یا payload خام ندارد.
/// </summary>
public sealed record AdminPaymentOpsView(
    Guid PaymentId,
    Guid CheckoutId,
    string Status,
    decimal Amount,
    string Currency,
    string ProviderCode,
    string? ProviderRequestReference,
    string? ProviderTransactionReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    string? LastFailureCode,
    bool ReconcileEligible);

/// <summary>
/// ردیف فروشنده از Party و شمارنده‌های جداگانهٔ Offer/Order.
/// </summary>
public sealed record AdminSellerListItem(
    Guid SellerPartyId,
    string DisplayName,
    string Status,
    int ActiveOffers,
    int OrderCount);

/// <summary>
/// مشتری صادقانهٔ عملیاتی بر پایهٔ User سفارش و آخرین snapshot گیرنده.
/// </summary>
public sealed record AdminCustomerListItem(
    Guid CustomerUserId,
    string DisplayName,
    string? ContactMobile,
    int OrderCount,
    DateTimeOffset LastOrderAt,
    string Status);
