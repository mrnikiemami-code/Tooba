using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Application.Admin.Detail.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Admin.Detail;

/// <summary>بارگذاری Checkout و ثبت CheckoutAdminViewAck با IClock.</summary>
internal sealed class AdminOrderDetailCheckoutStore : IAdminOrderDetailCheckoutStore
{
    private readonly OrderDbContext _orders;
    private readonly IClock _clock;

    public AdminOrderDetailCheckoutStore(OrderDbContext orders, IClock clock)
    {
        _orders = orders;
        _clock = clock;
    }

    public async Task<CheckoutGroup?> GetCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    public async Task RecordAdminViewAckAsync(
        Guid checkoutId,
        Guid viewerUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (viewerUserId == Guid.Empty)
        {
            return;
        }

        // Prefer injected now; fall back to IClock for callers that pass default.
        var at = now == default ? _clock.UtcNow : now;
        _orders.AdminViewAcks.Add(CheckoutAdminViewAck.Create(checkoutId, viewerUserId, at));
        await _orders.SaveChangesAsync(cancellationToken);
    }
}
