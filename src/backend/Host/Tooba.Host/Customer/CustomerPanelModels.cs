using Tooba.Order.Application.Customer.Models;

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
    long AddressBookCount,
    IReadOnlyList<CustomerOrderListItem> RecentOrders);

/// <summary>
/// پروفایل مشتری با مرز واضح بین فیلدهای توصیفی قابل‌ویرایش و شناسه‌های Identity.
/// </summary>
public sealed record CustomerProfilePage(
    Guid ActorUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? ContactMobile,
    string? BirthDate,
    string? Bio,
    string? LastShippingAddress,
    bool EmailEditable,
    bool MobileEditable,
    bool AvatarUploadAvailable,
    bool NationalCodeEditable,
    bool AddressEditable,
    bool Editable);
