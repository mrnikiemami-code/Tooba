using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Seller.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Seller;

/// <summary>خواندن SellerOrder با فیلتر مالکیت SellerPartyId.</summary>
internal sealed class SellerOrderStore(OrderDbContext orders) : ISellerOrderStore
{
    public async Task<IReadOnlyList<SellerOrder>> ListBySellerAsync(
        Guid sellerPartyId,
        int take,
        CancellationToken cancellationToken) =>
        await orders.SellerOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.SellerOrderId)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SellerOrder>> ListAllBySellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken) =>
        await orders.SellerOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.SellerPartyId == sellerPartyId)
            .ToListAsync(cancellationToken);

    public async Task<SellerOrder?> GetOwnedAsync(
        Guid sellerPartyId,
        Guid sellerOrderId,
        CancellationToken cancellationToken) =>
        await orders.SellerOrders.AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(
                x => x.SellerOrderId == sellerOrderId && x.SellerPartyId == sellerPartyId,
                cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, CheckoutGroup>> GetCheckoutsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
        {
            return new Dictionary<Guid, CheckoutGroup>();
        }

        return await orders.Checkouts.AsNoTracking()
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToDictionaryAsync(x => x.CheckoutId, cancellationToken);
    }

    public async Task<CheckoutGroup?> GetCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken) =>
        await orders.Checkouts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
}
