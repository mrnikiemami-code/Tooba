#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Storefront;

public sealed class StorefrontShippingDraftStore(OrderDbContext db) : IStorefrontShippingDraftStore
{
    public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
        db.ShippingDrafts.FirstOrDefaultAsync(x => x.CartId == cartId, cancellationToken);

    public async Task SaveAsync(CartShippingDraft draft, bool isNew, CancellationToken cancellationToken)
    {
        if (isNew)
        {
            db.ShippingDrafts.Add(draft);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class StorefrontPendingCheckoutStore(OrderDbContext db) : IStorefrontPendingCheckoutStore
{
    public async Task<IReadOnlyList<CheckoutGroup>> ListOwnedByUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken) =>
        await db.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => x.PlacedByUserId == userId)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CheckoutGroup>> ListByCheckoutIdsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken) =>
        await db.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);

    public Task<CheckoutGroup?> GetWithSellerOrdersAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        db.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    public async Task HidePendingCardAsync(
        Guid checkoutId,
        Guid? ownerUserId,
        Guid? guestCartId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var exists = await db.PendingPaymentCardHides.AsNoTracking()
            .AnyAsync(
                x => x.CheckoutId == checkoutId
                    && (ownerUserId != null
                        ? x.OwnerUserId == ownerUserId
                        : x.GuestCartId == guestCartId),
                cancellationToken);
        if (!exists)
        {
            db.PendingPaymentCardHides.Add(
                PendingPaymentCardHide.Create(checkoutId, ownerUserId, guestCartId, now));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<HashSet<Guid>> LoadHiddenCheckoutIdsAsync(
        IReadOnlyList<Guid> checkoutIds,
        Guid? ownerUserId,
        IReadOnlyList<Guid> guestCartIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
        {
            return [];
        }

        if (ownerUserId is Guid userId)
        {
            var rows = await db.PendingPaymentCardHides.AsNoTracking()
                .Where(x => x.OwnerUserId == userId && checkoutIds.Contains(x.CheckoutId))
                .Select(x => x.CheckoutId)
                .ToListAsync(cancellationToken);
            return [.. rows];
        }

        var guestRows = await db.PendingPaymentCardHides.AsNoTracking()
            .Where(x =>
                x.GuestCartId != null
                && guestCartIds.Contains(x.GuestCartId.Value)
                && checkoutIds.Contains(x.CheckoutId))
            .Select(x => x.CheckoutId)
            .ToListAsync(cancellationToken);
        return [.. guestRows];
    }
}
