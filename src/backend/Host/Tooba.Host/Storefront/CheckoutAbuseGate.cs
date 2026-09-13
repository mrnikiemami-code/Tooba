using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Host.Storefront;

/// <summary>
/// اعمال سقف سفارش باز و سهمیه Cycle #1 روی Store+CustomerId با قفل سطری قبل از رزرو.
/// </summary>
public sealed class CheckoutAbuseGate : ICheckoutAbuseGate
{
    private readonly OrderDbContext _orders;
    private readonly CatalogDbContext _catalog;

    /// <summary>سازنده.</summary>
    public CheckoutAbuseGate(OrderDbContext orders, CatalogDbContext catalog)
    {
        _orders = orders;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<CheckoutAbuseSettingsSnapshot> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var row = await _catalog.StoreCheckoutAbuseSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutAbuseSettings.SingletonId, cancellationToken);
        return ToSnapshot(row);
    }

    /// <inheritdoc />
    public async Task EnsureCanStartInitialReservationAsync(
        Guid placedByUserId,
        CheckoutAbuseSettingsSnapshot settings,
        CancellationToken cancellationToken)
    {
        if (!IsCustomer(placedByUserId))
        {
            return;
        }

        await AcquireCustomerLockAsync(placedByUserId, DateTimeOffset.UtcNow, cancellationToken);
        var openCount = await CountOpenUnpaidAsync(placedByUserId, cancellationToken);
        if (openCount >= settings.MaxOpenUnpaidOrdersPerCustomer)
        {
            throw new CheckoutAbuseLimitException(
                "checkout.open_unpaid_limit_reached",
                "open_unpaid",
                settings.StoreId,
                placedByUserId,
                openCount,
                settings.MaxOpenUnpaidOrdersPerCustomer,
                nextAvailableAt: null);
        }

        var windowStart = DateTimeOffset.UtcNow.AddMinutes(-settings.ReservationCommitWindowMinutes);
        var churnRows = await _orders.CheckoutReservationCommits.AsNoTracking()
            .Where(x => x.StoreId == settings.StoreId
                        && x.CustomerId == placedByUserId
                        && x.OccurredAt >= windowStart)
            .OrderBy(x => x.OccurredAt)
            .Select(x => x.OccurredAt)
            .ToListAsync(cancellationToken);
        if (churnRows.Count >= settings.MaxCheckoutCommitsPerCustomerInWindow)
        {
            var nextAt = churnRows[0].AddMinutes(settings.ReservationCommitWindowMinutes);
            throw new CheckoutAbuseLimitException(
                "checkout.reservation_commit_limit_reached",
                "reservation_commit",
                settings.StoreId,
                placedByUserId,
                churnRows.Count,
                settings.MaxCheckoutCommitsPerCustomerInWindow,
                nextAt);
        }
    }

    /// <inheritdoc />
    public void PrepareInitialCommit(
        Guid placedByUserId,
        Guid checkoutId,
        Guid orderId,
        CheckoutAbuseSettingsSnapshot settings,
        DateTimeOffset now)
    {
        if (!IsCustomer(placedByUserId))
        {
            return;
        }

        _orders.CheckoutReservationCommits.Add(
            CheckoutReservationCommit.Create(
                settings.StoreId,
                placedByUserId,
                orderId,
                checkoutId,
                now,
                "order-commit"));
    }

    /// <inheritdoc />
    public async Task RecordBlockAsync(CheckoutAbuseLimitException exception, CancellationToken cancellationToken)
    {
        _orders.ChangeTracker.Clear();
        _orders.CheckoutAbuseBlockEvents.Add(
            CheckoutAbuseBlockEvent.Create(
                exception.StoreId,
                exception.CustomerId,
                exception.Kind,
                exception.CurrentCount,
                exception.MaxCount,
                DateTimeOffset.UtcNow));
        await _orders.SaveChangesAsync(cancellationToken);
    }

    private async Task AcquireCustomerLockAsync(
        Guid customerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var row = await _orders.CheckoutAbuseCustomerLocks
            .SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
        if (row is null)
        {
            var created = new CheckoutAbuseCustomerLock { CustomerId = customerId, TouchedAt = now };
            _orders.CheckoutAbuseCustomerLocks.Add(created);
            try
            {
                await _orders.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException)
            {
                _orders.Entry(created).State = EntityState.Detached;
                row = await _orders.CheckoutAbuseCustomerLocks
                    .SingleAsync(x => x.CustomerId == customerId, cancellationToken);
            }
        }

        row.TouchedAt = now;
        await _orders.SaveChangesAsync(cancellationToken);
    }

    private Task<int> CountOpenUnpaidAsync(Guid customerId, CancellationToken cancellationToken) =>
        _orders.SellerOrders
            .Where(order => OpenUnpaidOrderPredicate.OpenUnpaidStatuses.Contains(order.Status))
            .Join(
                _orders.Checkouts,
                order => order.CheckoutId,
                checkout => checkout.CheckoutId,
                (order, checkout) => checkout)
            .CountAsync(checkout => checkout.PlacedByUserId == customerId, cancellationToken);

    private static bool IsCustomer(Guid placedByUserId) =>
        placedByUserId != Guid.Empty && placedByUserId != StorefrontCheckoutComposer.StorefrontGuestActorId;

    private static CheckoutAbuseSettingsSnapshot ToSnapshot(StoreCheckoutAbuseSettings? row) =>
        new(
            StoreCheckoutAbuseSettings.SingletonId,
            row?.MaxOpenUnpaidOrdersPerCustomer ?? StoreCheckoutAbuseSettings.DefaultMaxOpenUnpaid,
            row?.ReservationCommitWindowMinutes ?? StoreCheckoutAbuseSettings.DefaultWindowMinutes,
            row?.MaxCheckoutCommitsPerCustomerInWindow ?? StoreCheckoutAbuseSettings.DefaultMaxCommits);
}
