using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Infrastructure.Admin.Fulfillment;

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
