namespace Tooba.Host.Customer;

/// <summary>
/// خلاصهٔ داشبورد مشتری از سفارش‌های متعلق به کاربر احراز‌شده.
/// آمار علاقه‌مندی و آدرس فقط وضعیت قابلیت را نشان می‌دهد و دادهٔ جعلی نمی‌سازد.
/// </summary>
public sealed record CustomerDashboardPage(
    Guid ActorUserId,
    string DisplayName,
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    bool WishlistAvailable,
    long WishlistCount,
    bool AddressBookAvailable,
    IReadOnlyList<CustomerOrderListItem> RecentOrders);

/// <summary>
/// پروفایل خواندنی مشتری بر پایهٔ اصل نشست و آخرین تصویر ارسال.
/// این مدل جای دفترچهٔ Party یا Identity را نمی‌گیرد.
/// </summary>
public sealed record CustomerProfilePage(
    Guid ActorUserId,
    string DisplayName,
    string? ContactMobile,
    string? LastShippingAddress,
    bool Editable);

/// <summary>
/// یک سفارش تجمیعی مشتری که ممکن است چند سفارش فروشنده داشته باشد.
/// </summary>
public sealed record CustomerOrderListItem(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    int SellerCount,
    int ItemCount,
    decimal PayableAmount,
    string Currency,
    string PaymentState,
    string Status);

/// <summary>
/// خط سفارش مشتری با مبلغ snapshot تاریخی و نام فروشنده.
/// </summary>
public sealed record CustomerOrderLineView(
    Guid OfferId,
    string Title,
    string SellerDisplayName,
    int Quantity,
    decimal UnitAmount,
    decimal LinePayable,
    string Currency);

/// <summary>
/// بخش فروشنده در جزئیات سفارش مشتری.
/// </summary>
public sealed record CustomerSellerOrderView(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    string PaymentState,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<CustomerOrderLineView> Lines);

/// <summary>
/// جزئیات سفارش فقط برای اصل احراز‌شده، همراه تصویر ارسال checkout.
/// </summary>
public sealed record CustomerOrderDetailPage(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    string Status,
    string PaymentState,
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
    IReadOnlyList<CustomerSellerOrderView> SellerOrders);
