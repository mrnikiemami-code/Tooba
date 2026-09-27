using MediatR;
using Tooba.AddressBook.Contracts.Ports;
using Tooba.CustomerProfile.Application.Models;
using Tooba.CustomerProfile.Application.Ports;
using Tooba.CustomerProfile.Contracts;
using Tooba.Order.Contracts.Customer;
using Tooba.Wishlist.Contracts;

namespace Tooba.CustomerProfile.Application.Queries.GetCustomerAccountDashboard;

/// <summary>
/// Customer-account dashboard presentation composition. Policy-free, persistence-free,
/// Contracts-only cross-module reads. Actor is server-trusted.
/// </summary>
public sealed record GetCustomerAccountDashboardQuery(Guid ActorUserId) : IRequest<CustomerDashboardPage>;

/// <summary>Aggregates Order/Wishlist/AddressBook/CustomerProfile Contracts snapshots into the dashboard DTO.</summary>
public sealed class GetCustomerAccountDashboardQueryHandler(
    ICustomerOrderDashboardSummaryPort orderSummary,
    IWishlistCountPort wishlist,
    IAddressBookCountPort addresses,
    ICustomerProfileDirectory profiles,
    ICustomerAccountDisplayTexts displayTexts)
    : IRequestHandler<GetCustomerAccountDashboardQuery, CustomerDashboardPage>
{
    /// <inheritdoc />
    public async Task<CustomerDashboardPage> Handle(
        GetCustomerAccountDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await orderSummary.GetAsync(request.ActorUserId, cancellationToken)
            ?? new CustomerOrderDashboardSummaryDto(0, 0, 0, [], null, null, null);
        var displayName = await ResolveDisplayNameAsync(
            request.ActorUserId,
            summary.LatestOrderRecipientDisplayName,
            cancellationToken);
        var wishlistCount = await wishlist.CountAsync(request.ActorUserId, cancellationToken);
        var addressCount = await addresses.CountAsync(request.ActorUserId, cancellationToken);
        return new CustomerDashboardPage(
            request.ActorUserId,
            displayName,
            summary.TotalOrders,
            summary.PendingOrders,
            summary.PaidOrders,
            WishlistAvailable: true,
            WishlistCount: wishlistCount,
            AddressBookAvailable: true,
            AddressBookCount: addressCount,
            summary.RecentOrders);
    }

    private async Task<string> ResolveDisplayNameAsync(
        Guid actorUserId,
        string? latestOrderRecipient,
        CancellationToken cancellationToken)
    {
        var stored = await profiles.GetAsync(actorUserId, cancellationToken);
        if (stored is not null && !string.IsNullOrWhiteSpace(stored.DisplayName))
        {
            return stored.DisplayName;
        }

        return string.IsNullOrWhiteSpace(latestOrderRecipient)
            ? displayTexts.DefaultDisplayName
            : latestOrderRecipient;
    }
}
