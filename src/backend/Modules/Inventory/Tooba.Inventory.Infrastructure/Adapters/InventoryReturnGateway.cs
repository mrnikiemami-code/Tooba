using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Returns;
using Tooba.Inventory.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Domain.Events;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Infrastructure.Adapters;

/// <summary>
/// restock مرجوعی از طریق schema inventory با dedup idempotency.
/// </summary>
public sealed class InventoryReturnGateway : IInventoryReturnGateway
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryUseCaseGuard _guard;
    private readonly IInventoryDirectory _directory;
    private readonly IClock _clock;

    /// <summary>
    /// gateway را به schema inventory وصل می‌کند.
    /// </summary>
    public InventoryReturnGateway(
        InventoryDbContext db,
        IInventoryUseCaseGuard guard,
        IInventoryDirectory directory,
        IClock clock)
    {
        _db = db;
        _guard = guard;
        _directory = directory;
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task RestockFromReturnAsync(
        Guid reservationId,
        decimal quantity,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReturnRestockInvalid);
        }

        var normalizedKey = idempotencyKey.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            throw new ContractOperationException(InventoryErrorCodes.ReturnRestockInvalid);
        }

        if (await _db.ReturnRestockInbox.AnyAsync(x => x.IdempotencyKey == normalizedKey, cancellationToken))
        {
            return;
        }

        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);

        if (quantity > reservation.Quantity)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReturnRestockInvalid);
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
                    throw new ContractOperationException(InventoryErrorCodes.ReturnRestockInvalid);
                }

                await _directory.ReleaseAsync(reservationId, cancellationToken);
                break;
            case StockReservationStatus.Released:
                break;
            default:
                throw new ContractOperationException(InventoryErrorCodes.ReturnRestockInvalid);
        }

        _db.ReturnRestockInbox.Add(new ReturnRestockInboxRecord
        {
            IdempotencyKey = normalizedKey,
            ReservationId = reservationId,
            Quantity = quantity,
            ProcessedAt = _clock.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
