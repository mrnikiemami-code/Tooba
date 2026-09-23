using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Customer;

/// <summary>مالکیت checkout برای مسیر مشتری Fulfillment.</summary>
public sealed class CustomerCheckoutOwnershipBridge : ICustomerCheckoutOwnershipReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل را به OrderDbContext وصل می‌کند.</summary>
    public CustomerCheckoutOwnershipBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<CustomerCheckoutOwnershipSnapshot?> GetAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var row = await _db.Checkouts.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .Select(x => new { x.CheckoutId, x.PlacedByUserId, x.CartId })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new CustomerCheckoutOwnershipSnapshot(row.CheckoutId, row.PlacedByUserId, row.CartId);
    }
}
