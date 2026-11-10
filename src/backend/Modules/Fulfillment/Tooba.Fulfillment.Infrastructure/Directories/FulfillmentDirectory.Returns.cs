using System.Data;

using Microsoft.EntityFrameworkCore;

using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>
/// TB-258: post-dispatch exception path owned by Fulfillment — non-delivery, physical return, origin receipt and the
/// quantity-safe resend allocation. This is deliberately NOT the pre-dispatch cancel path and NOT the customer RMA
/// path after a successful delivery.
/// </summary>
public sealed partial class FulfillmentDirectory
{
    private const int ResendAllocationAttempts = 2;

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> RecordNonDeliveryAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string reason,
        string? note,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.RecordShipmentNonDelivery(shipmentId, reason, note, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("shipment_nondelivery");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> StartReturnAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.StartShipmentReturn(shipmentId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("shipment_return_started");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> RecordReturnReceiptAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        IReadOnlyList<ReturnReceiptLineCommand> items,
        string? note,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);

        var existing = unit.ReturnReceipts.SingleOrDefault(x => x.ShipmentId == shipmentId);
        if (existing is not null)
        {
            EnsureReceiptMatches(existing, items);
            return await MapSnapshotAsync(unit, cancellationToken);
        }

        var created = unit.RecordReturnReceipt(
            _ids.NewId(),
            () => _ids.NewId(),
            shipmentId,
            actorUserId,
            items.Select(x => (x.OrderLineId, x.Quantity, x.Condition)).ToArray(),
            _clock.UtcNow,
            note,
            _clock.UtcNow);
        // The receipt collection is deliberately unmapped (no phantom shadow FK), so the owning directory persists
        // the aggregate through its own DbSets — exactly like shipments and shipping groups.
        _db.ShipmentReturnReceipts.Add(created);
        _db.ShipmentReturnReceiptItems.AddRange(created.Items);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsReturnReceiptUniqueViolation(ex))
        {
            // A concurrent operator confirmed the same physical receipt first. The database unique index is the
            // exactly-once authority; replaying the identical receipt reconciles it, any different payload is a
            // typed conflict and never a second receipt.
            _db.ChangeTracker.Clear();
            var reloaded = await LoadMutableAsync(fulfillmentId, cancellationToken);
            var winner = reloaded.ReturnReceipts.Single(x => x.ShipmentId == shipmentId);
            EnsureReceiptMatches(winner, items);
            _telemetry.RecordTransition("shipment_return_received");
            return await MapSnapshotAsync(reloaded, cancellationToken);
        }

        _telemetry.RecordTransition("shipment_return_received");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentShipmentCreateResult> CreateResendShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        Guid originalShipmentId,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingDispatchKey = null)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);

        for (var attempt = 0; attempt < ResendAllocationAttempts; attempt++)
        {
            // Serializable is the allocation guard: two independent operators may both read the same returned
            // capacity, but only one of the two inserts can commit — the other gets a serialization failure and is
            // told, instead of silently consuming the same returned units twice.
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
            var existingByKey = FindResendByDispatchKey(unit, shippingDispatchKey);
            if (existingByKey is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FulfillmentShipmentCreateResult(
                    await MapSnapshotAsync(unit, cancellationToken),
                    existingByKey.ShipmentId);
            }

            var shipment = unit.CreateResendShipment(
                _ids.NewId(),
                () => _ids.NewId(),
                originalShipmentId,
                string.Empty,
                items.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
                _clock.UtcNow,
                null,
                null,
                shippingDispatchKey);

            _db.Shipments.Add(shipment);
            _db.ShipmentItems.AddRange(shipment.Items);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsResendDispatchKeyUniqueViolation(ex))
            {
                await SafeRollbackAsync(transaction);
                _db.ChangeTracker.Clear();
                continue;
            }
            catch (DbUpdateException ex) when (IsSerializationFailure(ex))
            {
                await SafeRollbackAsync(transaction);
                _db.ChangeTracker.Clear();
                throw new ContractOperationException("fulfillment.resend.concurrency_conflict", ex);
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == Npgsql.PostgresErrorCodes.SerializationFailure)
            {
                await SafeRollbackAsync(transaction);
                _db.ChangeTracker.Clear();
                throw new ContractOperationException("fulfillment.resend.concurrency_conflict", ex);
            }

            _telemetry.RecordShipmentCreated();
            return new FulfillmentShipmentCreateResult(
                await MapSnapshotAsync(unit, cancellationToken),
                shipment.ShipmentId);
        }

        throw new ContractOperationException("fulfillment.resend.concurrency_conflict");
    }

    /// <summary>
    /// TB-258: a failed statement inside an explicit transaction already completes the EF/Npgsql transaction, so a
    /// second explicit rollback would mask the real typed outcome with an InvalidOperationException.
    /// </summary>
    private static async Task SafeRollbackAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // The data provider completed the transaction when the failing statement aborted it.
        }
    }

    private static Shipment? FindResendByDispatchKey(FulfillmentUnit unit, string? shippingDispatchKey)
    {
        var key = string.IsNullOrWhiteSpace(shippingDispatchKey) ? null : shippingDispatchKey.Trim();
        if (key is null)
        {
            return null;
        }

        return unit.Shipments.FirstOrDefault(s =>
            s.IsResend
            && s.Status != ShipmentStatus.Cancelled
            && string.Equals(s.ShippingDispatchKey, key, StringComparison.Ordinal));
    }

    private static void EnsureReceiptMatches(
        ShipmentReturnReceipt receipt,
        IReadOnlyList<ReturnReceiptLineCommand> requested)
    {
        var recorded = receipt.Items
            .Select(x => (x.OrderLineId, x.Quantity, x.Condition))
            .OrderBy(x => x.OrderLineId)
            .ThenBy(x => x.Condition)
            .ToArray();
        var incoming = requested
            .Select(x => (x.OrderLineId, x.Quantity, x.Condition))
            .OrderBy(x => x.OrderLineId)
            .ThenBy(x => x.Condition)
            .ToArray();
        if (recorded.Length != incoming.Length || !recorded.SequenceEqual(incoming))
        {
            throw new ContractOperationException("fulfillment.return.receipt_conflict");
        }
    }

    /// <summary>Exact uniqueness fault of the per-parcel receipt index only.</summary>
    private static bool IsReturnReceiptUniqueViolation(DbUpdateException exception)
        => exception.InnerException is Npgsql.PostgresException
        {
            SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_shipment_return_receipts_shipment",
        };

    private static bool IsResendDispatchKeyUniqueViolation(DbUpdateException exception)
        => exception.InnerException is Npgsql.PostgresException
        {
            SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_shipments_shipping_dispatch_key",
        };

    private static bool IsSerializationFailure(DbUpdateException exception)
        => exception.InnerException is Npgsql.PostgresException
        {
            SqlState: Npgsql.PostgresErrorCodes.SerializationFailure,
        };
}
