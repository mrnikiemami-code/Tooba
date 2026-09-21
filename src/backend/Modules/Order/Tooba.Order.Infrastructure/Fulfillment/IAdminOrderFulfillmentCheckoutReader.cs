namespace Tooba.Order.Infrastructure.Fulfillment;

/// <summary>Checkout gate for admin fulfillment ops without Host types.</summary>
public interface IAdminOrderFulfillmentCheckoutReader
{
    /// <summary>Load checkout cancel/membership snapshot; null when missing.</summary>
    Task<AdminOrderFulfillmentCheckoutSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken);
}

/// <summary>Minimal checkout facts for fulfillment admin ops.</summary>
public sealed record AdminOrderFulfillmentCheckoutSnapshot(
    bool IsCancelled,
    IReadOnlyList<Guid> SellerOrderIds);
