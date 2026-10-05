using Tooba.Order.Contracts.Customer;

namespace Tooba.CustomerProfile.Application.Account.Models;

/// <summary>
/// Customer-account dashboard presentation DTO (JSON field parity with former Host CustomerDashboardPage).
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
    IReadOnlyList<CustomerOrderListItemDto> RecentOrders);

/// <summary>
/// Customer profile presentation DTO (JSON field parity with former Host CustomerProfilePage).
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
