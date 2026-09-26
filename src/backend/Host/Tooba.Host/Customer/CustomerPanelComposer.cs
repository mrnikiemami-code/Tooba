using Tooba.AddressBook.Application.Customer.Ports;
using Tooba.CustomerProfile.Application;
using Tooba.Identity.Application;
using Tooba.Order.Application.Customer.Models;
using Tooba.Wishlist.Application;

namespace Tooba.Host.Customer;

/// <summary>
/// ترکیب نازک داشبورد/پروفایل مشتری با Wishlist، AddressBook و Profile.
/// شمارنده‌ها و سفارش‌های اخیر فقط از خلاصهٔ Order CQRS تزریق می‌شوند.
/// </summary>
public sealed class CustomerPanelComposer
{
    private readonly IWishlistDirectory _wishlist;
    private readonly IAddressBookDirectory _addresses;
    private readonly ICustomerProfileDirectory _profiles;
    private readonly IIdentityContactLookup _identityContacts;

    /// <summary>
    /// ترکیب‌گر را با مرزهای Wishlist/AddressBook/Profile می‌سازد (بدون وابستگی به Order persistence).
    /// </summary>
    public CustomerPanelComposer(
        IWishlistDirectory wishlist,
        IAddressBookDirectory addresses,
        ICustomerProfileDirectory profiles,
        IIdentityContactLookup identityContacts)
    {
        _wishlist = wishlist;
        _addresses = addresses;
        _profiles = profiles;
        _identityContacts = identityContacts;
    }

    /// <summary>
    /// داشبورد واقعی مشتری را از خلاصهٔ Order + Wishlist/AddressBook می‌سازد.
    /// </summary>
    public async Task<CustomerDashboardPage> ComposeDashboardAsync(
        Guid actorUserId,
        CustomerOrderDashboardSummary orderSummary,
        CancellationToken cancellationToken)
    {
        var displayName = await ResolveDisplayNameAsync(
            actorUserId,
            orderSummary.LatestOrderRecipientDisplayName,
            cancellationToken);
        var wishlistCount = await _wishlist.CountAsync(actorUserId, cancellationToken);
        var addressCount = await _addresses.CountAsync(actorUserId, cancellationToken);
        return new CustomerDashboardPage(
            actorUserId,
            displayName,
            orderSummary.TotalOrders,
            orderSummary.PendingOrders,
            orderSummary.PaidOrders,
            WishlistAvailable: true,
            WishlistCount: wishlistCount,
            AddressBookAvailable: true,
            AddressBookCount: addressCount,
            orderSummary.RecentOrders);
    }

    /// <summary>
    /// پروفایل مشتری را از ماژول پروفایل و lookupهای Identity + کمک Order summary ترکیب می‌کند.
    /// </summary>
    public async Task<CustomerProfilePage> ComposeProfileAsync(
        Guid actorUserId,
        CustomerOrderDashboardSummary? orderSummary,
        CancellationToken cancellationToken)
    {
        var stored = await _profiles.GetAsync(actorUserId, cancellationToken);
        var contact = await _identityContacts.GetContactAsync(actorUserId, cancellationToken);
        var address = orderSummary?.LatestShippingAddress;
        var displayName = stored?.DisplayName
            ?? (string.IsNullOrWhiteSpace(orderSummary?.LatestOrderRecipientDisplayName)
                ? null
                : orderSummary!.LatestOrderRecipientDisplayName)
            ?? "مشتری توبا";
        var mobile = contact.Mobile
            ?? (string.IsNullOrWhiteSpace(orderSummary?.LatestContactMobile)
                ? null
                : orderSummary!.LatestContactMobile);
        return new CustomerProfilePage(
            actorUserId,
            displayName,
            stored?.FirstName,
            stored?.LastName,
            contact.Email,
            mobile,
            stored?.BirthDate,
            stored?.Bio,
            address,
            EmailEditable: false,
            MobileEditable: false,
            AvatarUploadAvailable: false,
            NationalCodeEditable: false,
            AddressEditable: false,
            Editable: true);
    }

    /// <summary>
    /// فیلدهای توصیفی مجاز پروفایل Actor را ذخیره می‌کند.
    /// </summary>
    public async Task UpsertProfileAsync(
        Guid actorUserId,
        CustomerProfileWrite input,
        CancellationToken cancellationToken) =>
        await _profiles.UpsertAsync(actorUserId, input, cancellationToken);

    private async Task<string> ResolveDisplayNameAsync(
        Guid actorUserId,
        string? latestOrderRecipient,
        CancellationToken cancellationToken)
    {
        var stored = await _profiles.GetAsync(actorUserId, cancellationToken);
        if (stored is not null && !string.IsNullOrWhiteSpace(stored.DisplayName))
        {
            return stored.DisplayName;
        }

        return string.IsNullOrWhiteSpace(latestOrderRecipient)
            ? "مشتری توبا"
            : latestOrderRecipient;
    }
}
