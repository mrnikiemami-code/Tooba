using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Infrastructure;

/// <summary>
/// restock مرجوعی از طریق schema inventory با dedup idempotency.
/// </summary>
public sealed class InventoryReturnGateway : IInventoryReturnGateway
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryUseCaseGuard _guard;
    private readonly IInventoryDirectory _directory;

    /// <summary>
    /// gateway را به schema inventory وصل می‌کند.
    /// </summary>
    public InventoryReturnGateway(
        InventoryDbContext db,
        IInventoryUseCaseGuard guard,
        IInventoryDirectory directory)
    {
        _db = db;
        _guard = guard;
        _directory = directory;
    }

    /// <inheritdoc />
    public async Task RestockFromReturnAsync(
        Guid reservationId,
        int quantity,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد restock باید مثبت باشد.");
        }

        var normalizedKey = idempotencyKey.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            throw new InvalidOperationException("کلید idempotency restock الزامی است.");
        }

        if (await _db.ReturnRestockInbox.AnyAsync(x => x.IdempotencyKey == normalizedKey, cancellationToken))
        {
            return;
        }

        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("رزرو پیدا نشد.");

        if (quantity > reservation.Quantity)
        {
            throw new InvalidOperationException("تعداد restock از رزرو بیشتر است.");
        }

        switch (reservation.Status)
        {
            case StockReservationStatus.Consumed:
                await _directory.AdjustAsync(
                    reservation.StockItemId,
                    StockAdjustmentKind.Increase,
                    quantity,
                    "return.restock",
                    normalizedKey,
                    cancellationToken);
                break;
            case StockReservationStatus.Held:
                if (quantity != reservation.Quantity)
                {
                    throw new InvalidOperationException("restock رزرو Held فقط با کل مقدار رزرو مجاز است.");
                }

                await _directory.ReleaseAsync(reservationId, cancellationToken);
                break;
            case StockReservationStatus.Released:
                break;
            default:
                throw new InvalidOperationException("restock برای وضعیت رزرو مجاز نیست.");
        }

        _db.ReturnRestockInbox.Add(new ReturnRestockInboxRecord
        {
            IdempotencyKey = normalizedKey,
            ReservationId = reservationId,
            Quantity = quantity,
            ProcessedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
