using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-272: ordinary (customer-delivered) creation. Authority first, then ONE classified decision per seller order.
/// <para>
/// The previous shape answered a globally matched idempotency key <b>before</b> it validated the submitted order and
/// before it compared what the stored case was actually created for, so a replay could hand back a case belonging to
/// another customer/order, and a changed request could be answered as if it were the original. The order of
/// operations here is therefore deliberate:
/// </para>
/// <list type="number">
/// <item><description>the caller's mutation gate, the submitted order binding and (customer path) ownership;</description></item>
/// <item><description>one normalized creation intent (lines as a set, trimmed reason, destination);</description></item>
/// <item><description>the relational critical section of the canonical seller order (serializes the quantity read);</description></item>
/// <item><description>exact replay answered only for the identical intent, otherwise a typed conflict with zero writes;</description></item>
/// <item><description>eligibility read AFTER the lock, then the insert.</description></item>
/// </list>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <summary>Persisted quantity scale of <c>returns.return_items.quantity</c> (numeric(18,6)).</summary>
    private const int QuantityScale = 6;

    private async Task<ReturnSnapshot> CreateInternalAsync(
        CreateReturnCommand command,
        bool requireOwner,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            throw new ContractOperationException(ReturnsErrorCodes.IdempotencyRequired);
        }

        // TB-272 (A): the immutable binding is loaded and validated FIRST. Nothing below may answer a stored
        // snapshot before this caller is proven to address this order (and, on the customer path, to own it).
        var orderContext = await _orders.GetReturnContextAsync(command.SellerOrderId, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);

        if (requireOwner && orderContext.PlacedByUserId != command.ActorUserId)
        {
            throw new ContractOperationException(ReturnsErrorCodes.NotOwner);
        }

        // TB-272 (B): one ordinary request line per order line — refused here (endpoint-reachable typed fault) and
        // again by the aggregate so no future caller can bypass it. Courier-origin creation is unaffected.
        if (ReturnRequest.FindRepeatedOrderLine(command.Items.Select(x => x.OrderLineId)) is not null)
        {
            throw new ContractOperationException(ReturnsErrorCodes.DuplicateLine);
        }

        var intent = OrdinaryCreationIntent.Capture(orderContext, command);
        var idempotencyKey = command.IdempotencyKey.Trim();

        // TB-272 (D): the whole decision (quantity read → insert) is one critical section per seller order.
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            if (relational)
            {
                await ReturnCreationSerialization.AcquireAsync(
                    _db.Database, orderContext.SellerOrderId, cancellationToken);
            }

            var stored = await _db.ReturnRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (stored is not null)
            {
                var replay = await AnswerStoredCreationAsync(stored, intent, cancellationToken);
                await CommitCreationAsync(transaction, ownsTransaction, cancellationToken);
                return replay;
            }

            // The quantity authority is read AFTER the lock, so it observes every creation that committed while this
            // request waited. Nothing external (money/stock/provider) runs inside this transaction.
            var eligibility = await _eligibility.EvaluateAsync(command.SellerOrderId, cancellationToken);
            if (!eligibility.Eligible)
            {
                throw new ContractOperationException(ReturnEligibilityReasonCodes.ToErrorCode(eligibility.ReasonCode));
            }

            var remainingByLine = eligibility.Lines.ToDictionary(x => x.OrderLineId, x => x.RemainingReturnableQuantity);
            var lineSnapshots = new List<(Guid OrderLineId, decimal Quantity, decimal UnitPriceSnapshot, Guid? ReservationId)>();
            foreach (var item in command.Items)
            {
                var orderLine = orderContext.Lines.SingleOrDefault(x => x.OrderLineId == item.OrderLineId)
                    ?? throw new ContractOperationException(ReturnsErrorCodes.LineMissing);
                remainingByLine.TryGetValue(item.OrderLineId, out var remaining);
                if (item.Quantity <= 0 || item.Quantity > remaining)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.QuantityExceeded);
                }

                lineSnapshots.Add((item.OrderLineId, item.Quantity, orderLine.UnitPriceSnapshot, orderLine.ReservationId));
            }

            var now = _clock.UtcNow;
            var request = ReturnRequest.Create(
                _ids.NewId(),
                () => _ids.NewId(),
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                orderContext.SellerPartyId,
                orderContext.PlacedByUserId,
                idempotencyKey,
                command.Reason,
                orderContext.Currency,
                lineSnapshots,
                now,
                command.RefundDestination);
            _db.ReturnRequests.Add(request);
            _db.ReturnItems.AddRange(request.Items);
            await _db.SaveChangesAsync(cancellationToken);
            await CommitCreationAsync(transaction, ownsTransaction, cancellationToken);
            _telemetry.RecordCreated();
            return await MapSnapshotAsync(request, cancellationToken);
        }
        catch (DbUpdateException ex) when (ReturnCreationSerialization.IsIdempotencyConflict(ex))
        {
            // ONLY the idempotency unique index reaches here (a genuine same-key collision that did not serialize, for
            // example a writer holding an outer transaction). The winner is re-read and compared with the SAME
            // authorization + exact-intent rule; a different intent stays a typed conflict. Anything else propagates.
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _db.ChangeTracker.Clear();
            var winner = await _db.ReturnRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken)
                ?? throw new ContractOperationException(ReturnsErrorCodes.Stale);
            return await AnswerStoredCreationAsync(winner, intent, cancellationToken);
        }
    }

    /// <summary>
    /// Answers an already-stored case for this key ONLY when the stored row is the same immutable creation intent
    /// (order, owning customer, destination, reason and the exact line/quantity set). A changed intent is a typed
    /// conflict, never a borrowed snapshot and never a mutation.
    /// </summary>
    private async Task<ReturnSnapshot> AnswerStoredCreationAsync(
        ReturnRequest stored,
        OrdinaryCreationIntent intent,
        CancellationToken cancellationToken)
    {
        var snapshot = await MapSnapshotAsync(stored, cancellationToken);
        if (!intent.Matches(stored, snapshot))
        {
            throw new ContractOperationException(ReturnsErrorCodes.IdempotencyConflict);
        }

        return snapshot;
    }

    private static async Task CommitCreationAsync(
        IDbContextTransaction? transaction,
        bool ownsTransaction,
        CancellationToken cancellationToken)
    {
        if (ownsTransaction && transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// The normalized creation intent of ONE ordinary request. Comparison is deliberately ordered and
    /// precision-normalized: a different item ORDER is the same intent, while a different line set, quantity,
    /// destination, reason, order or owning customer is a different request.
    /// </summary>
    private sealed record OrdinaryCreationIntent(
        Guid SellerOrderId,
        Guid RequestedByUserId,
        RefundDestination RefundDestination,
        string ReasonKey,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> Lines)
    {
        public static OrdinaryCreationIntent Capture(
            OrderReturnContextSnapshot orderContext,
            CreateReturnCommand command) =>
            new(
                orderContext.SellerOrderId,
                orderContext.PlacedByUserId,
                command.RefundDestination,
                NormalizeReason(command.Reason),
                command.Items
                    .Select(x => (x.OrderLineId, NormalizeQuantity(x.Quantity)))
                    .OrderBy(x => x.OrderLineId)
                    .ToArray());

        public bool Matches(ReturnRequest stored, ReturnSnapshot snapshot)
        {
            if (stored.SellerOrderId != SellerOrderId
                || stored.Origin != ReturnOrigin.CustomerDelivered
                || stored.RequestedByUserId != RequestedByUserId
                || stored.RefundDestination != RefundDestination
                || !string.Equals(NormalizeReason(stored.Reason), ReasonKey, StringComparison.Ordinal))
            {
                return false;
            }

            var storedLines = snapshot.Items
                .Select(x => (x.OrderLineId, NormalizeQuantity(x.Quantity)))
                .OrderBy(x => x.OrderLineId)
                .ToArray();
            return storedLines.Length == Lines.Count && storedLines.SequenceEqual(Lines);
        }

        private static string NormalizeReason(string? reason) => (reason ?? string.Empty).Trim();

        private static decimal NormalizeQuantity(decimal quantity) =>
            decimal.Round(quantity, QuantityScale, MidpointRounding.AwayFromZero);
    }
}
