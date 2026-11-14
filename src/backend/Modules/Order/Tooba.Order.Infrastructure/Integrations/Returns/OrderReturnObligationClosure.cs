using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Returns;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Returns;

/// <summary>
/// TB-265-R1: Order-owned authority over the remaining customer delivery obligation of one line when a courier-origin
/// Returns case is approved.
/// <para>
/// The approval is recorded as a DURABLE row (<c>order.order_line_obligation_closures</c>) through this module's own
/// persistence seam. It authors the decision and identifies the exact held reservation of that line; it NEVER
/// decrements ordered, packed or shipped history (a returned courier parcel is not an "unshipped" parcel) and it never
/// releases stock itself. The caller closes the branch and releases only the unconsumed held quantity once, through
/// the owning Inventory seam.
/// </para>
/// <para>
/// Exact identity is the contract: the same case + line + claim + quantity replays the recorded row with zero writes,
/// a changed quantity/claim under the same case is a typed conflict, and the cumulative closed quantity of a line can
/// never exceed what that line still owes. Concurrent approvals serialize on the order-line row.
/// </para>
/// </summary>
public sealed class OrderReturnObligationClosure : IOrderReturnObligationClosure
{
    /// <summary>Reason code: the same case line was asked to close a different quantity/claim.</summary>
    public const string IntentConflictCode = "order.return.closure_intent_conflict";

    private readonly IOrderReturnReader _orders;
    private readonly OrderDbContext _db;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>Wires the closure to the Order-owned snapshot read model and its durable persistence.</summary>
    public OrderReturnObligationClosure(
        IOrderReturnReader orders,
        OrderDbContext db,
        IClock clock,
        IIdGenerator ids)
    {
        _orders = orders;
        _db = db;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public async Task<OrderLineObligationClosureResult> CloseLineObligationAsync(
        OrderLineObligationClosureRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SellerOrderId == Guid.Empty || request.OrderLineId == Guid.Empty)
        {
            return Refused(request.OrderLineId, "order.return.source_required");
        }

        if (request.ReturnRequestId == Guid.Empty || request.SourceClaimId == Guid.Empty)
        {
            // A closure without its owning case/claim identity could never be replayed or reconciled.
            return Refused(request.OrderLineId, "order.return.source_required");
        }

        if (request.ActorUserId == Guid.Empty)
        {
            return Refused(request.OrderLineId, "order.return.actor_required");
        }

        if (request.Quantity <= 0m)
        {
            return Refused(request.OrderLineId, "order.return.quantity_invalid");
        }

        var context = await _orders.GetReturnContextAsync(request.SellerOrderId, cancellationToken);
        if (context is null)
        {
            return Refused(request.OrderLineId, "return.missing");
        }

        if (context.CheckoutId != request.CheckoutId)
        {
            return Refused(request.OrderLineId, "order.return.checkout_mismatch");
        }

        if (!context.IsPaid)
        {
            // A never-paid order has no money to refund and no delivered obligation to close.
            return Refused(request.OrderLineId, "return.not_paid");
        }

        var line = context.Lines.SingleOrDefault(x => x.OrderLineId == request.OrderLineId);
        if (line is null)
        {
            return Refused(request.OrderLineId, "return.line_missing");
        }

        // Serialize competing approvals of the same line on the owning order-line row, then decide from what is
        // actually durably closed. Without this lock two approvals could both pass a stale remainder read.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (ownsTransaction)
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM \"order\".order_lines WHERE line_id = {request.OrderLineId} FOR UPDATE",
                cancellationToken);
        }

        var recorded = await _db.ReturnObligationClosures
            .SingleOrDefaultAsync(x => x.ReturnRequestId == request.ReturnRequestId
                                       && x.OrderLineId == request.OrderLineId, cancellationToken);

        if (recorded is not null)
        {
            // Exact replay answers the FIRST durable closure. A changed quantity/claim is a different decision over
            // the same case line: it is refused BEFORE any write instead of silently re-closing more obligation.
            var sameIntent = recorded.SourceClaimId == request.SourceClaimId
                             && recorded.Quantity == request.Quantity
                             && recorded.SellerOrderId == request.SellerOrderId
                             && recorded.CheckoutId == request.CheckoutId;
            var closedTotal = await ClosedTotalAsync(request.OrderLineId, cancellationToken);
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return sameIntent
                ? new OrderLineObligationClosureResult(
                    true,
                    request.OrderLineId,
                    recorded.Quantity,
                    RemainingOf(line.Quantity, closedTotal),
                    line.ReservationId,
                    null,
                    recorded.ClosureId)
                : Refused(request.OrderLineId, IntentConflictCode);
        }

        var alreadyClosed = await ClosedTotalAsync(request.OrderLineId, cancellationToken);
        var remainingBefore = line.Quantity - alreadyClosed;
        if (remainingBefore <= 0m || request.Quantity > remainingBefore)
        {
            // Sibling portions and previously closed deliveries are never closed twice.
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return Refused(request.OrderLineId, "order.return.quantity_exceeded");
        }

        var closure = OrderLineObligationClosureRecord.Create(
            _ids.NewId(),
            request.SellerOrderId,
            request.CheckoutId,
            request.OrderLineId,
            request.ReturnRequestId,
            request.SourceClaimId,
            request.Quantity,
            request.ActorUserId,
            _clock.UtcNow);

        _db.ReturnObligationClosures.Add(closure);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _db.Entry(closure).State = EntityState.Detached;
            // The unique (case, line, claim) guard is the real concurrency safety: a racing approval loses here and
            // reads the winner instead of closing the same obligation twice.
            var winner = await _db.ReturnObligationClosures.AsNoTracking()
                .SingleOrDefaultAsync(x => x.ReturnRequestId == request.ReturnRequestId
                                           && x.OrderLineId == request.OrderLineId, cancellationToken);
            if (winner is not null && winner.SourceClaimId == request.SourceClaimId && winner.Quantity == request.Quantity)
            {
                var total = await ClosedTotalAsync(request.OrderLineId, cancellationToken);
                return new OrderLineObligationClosureResult(
                    true,
                    request.OrderLineId,
                    winner.Quantity,
                    RemainingOf(line.Quantity, total),
                    line.ReservationId,
                    null,
                    winner.ClosureId);
            }

            return Refused(request.OrderLineId, IntentConflictCode);
        }

        return new OrderLineObligationClosureResult(
            true,
            request.OrderLineId,
            request.Quantity,
            RemainingOf(line.Quantity, alreadyClosed + request.Quantity),
            line.ReservationId,
            null,
            closure.ClosureId);
    }

    /// <inheritdoc />
    public async Task<OrderLineObligationClosureSnapshot?> GetClosureAsync(
        Guid returnRequestId,
        Guid orderLineId,
        CancellationToken cancellationToken)
    {
        if (returnRequestId == Guid.Empty || orderLineId == Guid.Empty)
        {
            return null;
        }

        var row = await _db.ReturnObligationClosures.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId && x.OrderLineId == orderLineId,
                cancellationToken);
        return row is null ? null : Map(row);
    }

    /// <inheritdoc />
    public async Task<decimal> GetClosedQuantityAsync(Guid orderLineId, CancellationToken cancellationToken)
    {
        if (orderLineId == Guid.Empty)
        {
            return 0m;
        }

        return await ClosedTotalAsync(orderLineId, cancellationToken);
    }

    private async Task<decimal> ClosedTotalAsync(Guid orderLineId, CancellationToken cancellationToken)
    {
        var total = await _db.ReturnObligationClosures.AsNoTracking()
            .Where(x => x.OrderLineId == orderLineId)
            .SumAsync(x => (decimal?)x.Quantity, cancellationToken);
        return total ?? 0m;
    }

    private static decimal RemainingOf(decimal lineQuantity, decimal closedTotal)
    {
        var remaining = lineQuantity - closedTotal;
        return remaining > 0m ? remaining : 0m;
    }

    private static async Task CommitAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        bool ownsTransaction,
        CancellationToken cancellationToken)
    {
        if (ownsTransaction && transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static OrderLineObligationClosureSnapshot Map(OrderLineObligationClosureRecord row) =>
        new(
            row.ClosureId,
            row.SellerOrderId,
            row.CheckoutId,
            row.OrderLineId,
            row.ReturnRequestId,
            row.SourceClaimId,
            row.Quantity,
            row.ActorUserId,
            row.ClosedAt);

    private static OrderLineObligationClosureResult Refused(Guid orderLineId, string blockerCode) =>
        new(false, orderLineId, 0m, 0m, null, blockerCode);
}
