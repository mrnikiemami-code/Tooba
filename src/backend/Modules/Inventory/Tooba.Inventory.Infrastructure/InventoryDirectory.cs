using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;

namespace Tooba.Inventory.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد. ماتریس انبار اینجا نیست.
/// </summary>
public sealed class OpenInventoryUseCaseGuard : IInventoryUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// نوشتن و خواندن موجودی با قرارداد Offer. DbContext کاتالوگ و Offer لمس نمی‌شود.
/// رزرو با UPDATE اتمی PostgreSQL است تا آخرین واحد دو بار فروخته نشود.
/// </summary>
public sealed class InventoryDirectory : IInventoryDirectory, IInventoryAvailabilityGateway
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryUseCaseGuard _guard;
    private readonly IOfferLookupGateway _offers;
    private readonly ICatalogLookupGateway _catalog;

    /// <summary>
    /// دایرکتوری را به schema Inventory و درز Offer/Catalog وصل می‌کند نه به join بین‌schema.
    /// </summary>
    public InventoryDirectory(
        InventoryDbContext db,
        IInventoryUseCaseGuard guard,
        IOfferLookupGateway offers,
        ICatalogLookupGateway catalog)
    {
        _db = db;
        _guard = guard;
        _offers = offers;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<InventoryAvailability?> GetAvailabilityAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var rows = await (
            from position in _db.Positions.AsNoTracking()
            join location in _db.Locations.AsNoTracking() on position.LocationId equals location.LocationId
            where position.OfferId == offerId
            select new { position, location }).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var locations = rows.Select(row => new LocationAvailability(
            row.location.LocationId,
            row.location.Code,
            row.position.OnHand,
            row.position.Reserved,
            row.position.OnHand - row.position.Reserved)).ToList();
        return new InventoryAvailability(
            offerId,
            rows[0].position.CatalogVariantId,
            locations.Sum(x => x.OnHand),
            locations.Sum(x => x.Reserved),
            locations.Sum(x => x.Available),
            locations);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateLocationAsync(string code, string name, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var location = InventoryLocation.Create(code, name, DateTimeOffset.UtcNow);
        _db.Locations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);
        return location.LocationId;
    }

    /// <inheritdoc />
    public async Task<Guid> OpenPositionAsync(Guid offerId, Guid locationId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await _offers.FindOfferAsync(offerId, cancellationToken)
            ?? throw new InvalidOperationException("Offer از قرارداد Lookup پیدا نشد؛ DbContext Offer خوانده نشد.");
        if (await _catalog.FindVariantAsync(offer.CatalogVariantId, cancellationToken) is null)
        {
            throw new InvalidOperationException("گونهٔ Catalog از قرارداد Lookup پیدا نشد؛ DbContext Catalog خوانده نشد.");
        }

        if (await _db.Locations.SingleOrDefaultAsync(x => x.LocationId == locationId, cancellationToken) is not { Status: InventoryLocationStatus.Active })
        {
            throw new InvalidOperationException("محل نگهداری فعال پیدا نشد.");
        }

        var existing = await _db.Positions.SingleOrDefaultAsync(
            x => x.OfferId == offerId && x.LocationId == locationId,
            cancellationToken);
        if (existing is not null)
        {
            return existing.StockItemId;
        }

        var position = StockPosition.Open(offerId, offer.CatalogVariantId, locationId, DateTimeOffset.UtcNow);
        _db.Positions.Add(position);
        await _db.SaveChangesAsync(cancellationToken);
        return position.StockItemId;
    }

    /// <inheritdoc />
    public async Task AdjustAsync(
        Guid stockItemId,
        StockAdjustmentKind kind,
        int quantity,
        string reason,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("اصلاح موجودی بدون دلیل عملیاتی مجاز نیست.");
        }

        if (quantity < 0)
        {
            throw new InvalidOperationException("مقدار اصلاح منفی نیست؛ کاهش از گونهٔ Decrease استفاده می‌کند.");
        }

        var position = await _db.Positions.SingleOrDefaultAsync(x => x.StockItemId == stockItemId, cancellationToken)
            ?? throw new InvalidOperationException("موقعیت موجودی پیدا نشد.");

        var delta = kind switch
        {
            StockAdjustmentKind.Increase => quantity,
            StockAdjustmentKind.Decrease => -quantity,
            StockAdjustmentKind.Set => quantity - position.OnHand,
            _ => throw new InvalidOperationException("گونهٔ اصلاح ناشناخته است."),
        };

        var now = DateTimeOffset.UtcNow;
        var affected = await _db.Positions
            .Where(x => x.StockItemId == stockItemId && x.OnHand + delta >= x.Reserved && x.OnHand + delta >= 0)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.OnHand, x => x.OnHand + delta)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (affected != 1)
        {
            throw new InvalidOperationException("اصلاح موجودی وضعیت غیرممکن می‌ساخت یا موقعیت همزمان تغییر کرد.");
        }

        await _db.Entry(position).ReloadAsync(cancellationToken);
        position.RecordAdjustment(kind, delta, reason.Trim());
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
        _ = idempotencyKey;
    }

    /// <inheritdoc />
    public async Task<ReservationReceipt> ReserveAsync(
        Guid stockItemId,
        int quantity,
        string? externalReference,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var prior = await _db.Reservations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey.Trim(), cancellationToken);
            if (prior is not null)
            {
                var known = await _db.Positions.AsNoTracking().SingleAsync(x => x.StockItemId == prior.StockItemId, cancellationToken);
                return new ReservationReceipt(prior.ReservationId, prior.StockItemId, known.OfferId, prior.Quantity, prior.Status);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var reserved = await _db.Positions
            .Where(x => x.StockItemId == stockItemId && x.OnHand - x.Reserved >= quantity && quantity > 0)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Reserved, x => x.Reserved + quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (reserved != 1)
        {
            throw new InvalidOperationException("موجودی قابل‌فروش برای رزرو کافی نیست یا درخواست همزمان آخرین واحد را برد.");
        }

        var position = await _db.Positions.SingleAsync(x => x.StockItemId == stockItemId, cancellationToken);
        var hold = StockReservation.Hold(stockItemId, quantity, externalReference, idempotencyKey, now);
        _db.Reservations.Add(hold);
        position.RecordReserved(hold.ReservationId, quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
        return new ReservationReceipt(hold.ReservationId, stockItemId, position.OfferId, quantity, hold.Status);
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("رزرو پیدا نشد.");
        var now = DateTimeOffset.UtcNow;
        var released = await _db.Positions
            .Where(x => x.StockItemId == reservation.StockItemId && x.Reserved >= reservation.Quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Reserved, x => x.Reserved - reservation.Quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (released != 1)
        {
            throw new InvalidOperationException("آزادسازی رزرو با موجودی هم‌خوان نبود.");
        }

        reservation.MoveTo(StockReservationStatus.Released, now);
        var position = await _db.Positions.SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        position.RecordReleased(reservation.ReservationId, reservation.Quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("رزرو پیدا نشد.");
        var now = DateTimeOffset.UtcNow;
        var consumed = await _db.Positions
            .Where(x => x.StockItemId == reservation.StockItemId
                        && x.Reserved >= reservation.Quantity
                        && x.OnHand >= reservation.Quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.OnHand, x => x.OnHand - reservation.Quantity)
                    .SetProperty(x => x.Reserved, x => x.Reserved - reservation.Quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (consumed != 1)
        {
            throw new InvalidOperationException("مصرف رزرو با موجودی هم‌خوان نبود.");
        }

        reservation.MoveTo(StockReservationStatus.Consumed, now);
        var position = await _db.Positions.SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        position.RecordConsumed(reservation.ReservationId, reservation.Quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
