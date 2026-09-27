namespace Tooba.Order.Contracts.Customer;

/// <summary>Stable customer-facing order list row for dashboard RecentOrders (JSON field parity).</summary>
public sealed record CustomerOrderListItemDto(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    int SellerCount,
    int ItemCount,
    decimal PayableAmount,
    string Currency,
    string PaymentState,
    string Status);

/// <summary>Order-owned dashboard summary snapshot + optional profile enrichment hints.</summary>
public sealed record CustomerOrderDashboardSummaryDto(
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    IReadOnlyList<CustomerOrderListItemDto> RecentOrders,
    string? LatestOrderRecipientDisplayName,
    string? LatestShippingAddress,
    string? LatestContactMobile);

/// <summary>Contracts-only read port for customer-account dashboard composition.</summary>
public interface ICustomerOrderDashboardSummaryPort
{
    /// <summary>Returns Order-owned dashboard summary for the actor, or null when actor is empty.</summary>
    Task<CustomerOrderDashboardSummaryDto?> GetAsync(Guid actorUserId, CancellationToken cancellationToken);
}
