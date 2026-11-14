using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Contracts.Operations;
using Tooba.Returns.Domain.ValueObjects;
using AppModels = Tooba.Returns.Application.ReturnRequests.Models;
using ReturnStatusOverlayRow = Tooba.Returns.Contracts.Operations.ReturnStatusOverlayRow;

namespace Tooba.Returns.Infrastructure.Adapters;

/// <summary>
/// Contract-facing adapter over <see cref="IReturnDirectory"/> and <see cref="IReturnEligibilityEvaluator"/>
/// so admin order callers never reference Returns Application/Domain types.
/// The module's internal port consumes the authoritative MediatR request shapes, so this adapter composes
/// the seller scope from the owning snapshot (admin callers never carry it) and never duplicates a
/// command record. Expected failures originate as <see cref="ContractOperationException"/> at the owning
/// Directory; this adapter does not parse Message or promote InvalidOperationException.
/// </summary>
internal sealed class ReturnAdminOperationsAdapter(
    IReturnDirectory directory,
    IReturnEligibilityEvaluator eligibility) : IReturnAdminOperations
{
    public async Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReturnSnapshot> list =
            (await directory.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken)).Select(Map).ToList();
        return list;
    }

    public async Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReturnStatusOverlayRow> list =
            (await directory.ListStatusOverlayAsync(cancellationToken))
            .Select(x => new ReturnStatusOverlayRow(x.SellerOrderId, (ReturnRequestOperationStatus)x.Status))
            .ToList();
        return list;
    }

    public async Task<ReturnEligibilityResult> EvaluateEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken) =>
        Map(await eligibility.EvaluateAsync(sellerOrderId, cancellationToken));

    public async Task<ReturnSnapshot> CreateAdminInitiatedAsync(
        CreateAdminReturnCommand command,
        CancellationToken cancellationToken) =>
        Map(await directory.CreateAdminInitiatedAsync(
            new CreateReturnCommand(
                command.SellerOrderId,
                command.ActorUserId,
                command.IdempotencyKey,
                command.Reason,
                command.Items.Select(x => new AppModels.ReturnLineCommand(x.OrderLineId, x.Quantity)).ToList(),
                RefundDestination.OriginalPayment),
            cancellationToken));

    public async Task<ReturnSnapshot> ApproveAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.ApproveAsync(
            new ApproveReturnCommand(
                returnRequestId,
                actorUserId,
                await RequireSellerPartyAsync(returnRequestId, cancellationToken)),
            cancellationToken));

    public async Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken) =>
        Map(await directory.RejectAsync(
            new RejectReturnCommand(
                returnRequestId,
                actorUserId,
                await RequireSellerPartyAsync(returnRequestId, cancellationToken),
                reason),
            cancellationToken));

    public async Task<ReturnSnapshot> RetryRefundAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.RetryRefundAsync(
            new RetryReturnRefundCommand(returnRequestId, actorUserId), cancellationToken));

    /// <summary>
    /// Admin callers address a return by identifier only; the authoritative command shape also carries
    /// the owning seller scope (used by the seller use case). The adapter resolves it from the owning
    /// snapshot so the same <c>return.missing</c> fault is raised for an unknown identifier.
    /// </summary>
    public async Task<CourierOriginReturnResult> CreateCourierOriginAsync(
        CreateCourierOriginReturnCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (snapshot, claimId, alreadyExisted, conflict) = await directory.CreateCourierOriginAsync(
            command.FulfillmentId,
            command.ShipmentId,
            command.ActorUserId,
            command.IdempotencyKey,
            command.OccurrenceKey,
            command.Reason,
            command.Items
                .Select(x => (
                    x.OrderLineId,
                    x.Quantity,
                    x.Condition == ReturnItemConditionOption.Damaged
                        ? ReturnItemCondition.Damaged
                        : ReturnItemCondition.Usable))
                .ToList(),
            cancellationToken);

        if (snapshot is null)
        {
            throw new ContractOperationException(conflict ?? ReturnsErrorCodes.Missing);
        }

        return new CourierOriginReturnResult(Map(snapshot), claimId, alreadyExisted, conflict);
    }

    public async Task<CourierOriginReturnResult?> ReconcileCourierOriginAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        string occurrenceKey,
        CancellationToken cancellationToken)
    {
        var (snapshot, claimId, alreadyExisted, conflict) = await directory.ReconcileCourierOriginAsync(
            fulfillmentId,
            shipmentId,
            occurrenceKey,
            cancellationToken);

        if (snapshot is null)
        {
            return conflict is null ? null : new CourierOriginReturnResult(
                new ReturnSnapshot(
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    ReturnRequestOperationStatus.Requested,
                    null,
                    string.Empty,
                    0m,
                    null,
                    RefundDestinationOption.OriginalPayment,
                    default,
                    default,
                    Array.Empty<ReturnItemSnapshot>(),
                    Array.Empty<RefundAttemptSnapshot>()),
                claimId,
                alreadyExisted,
                conflict);
        }

        return new CourierOriginReturnResult(Map(snapshot), claimId, alreadyExisted, conflict);
    }

    private async Task<Guid> RequireSellerPartyAsync(Guid returnRequestId, CancellationToken cancellationToken) =>
        (await directory.GetAsync(returnRequestId, cancellationToken))?.SellerPartyId
        ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);

    private static ReturnSnapshot Map(AppModels.ReturnSnapshot snapshot) =>
        new(
            snapshot.ReturnRequestId,
            snapshot.SellerOrderId,
            snapshot.CheckoutId,
            snapshot.SellerPartyId,
            snapshot.RequestedByUserId,
            (ReturnRequestOperationStatus)snapshot.Status,
            snapshot.Reason,
            snapshot.Currency,
            snapshot.RefundAmount,
            snapshot.PaymentId,
            (RefundDestinationOption)snapshot.RefundDestination,
            snapshot.CreatedAt,
            snapshot.UpdatedAt,
            snapshot.Items
                .Select(x => new ReturnItemSnapshot(
                    x.ReturnItemId,
                    x.OrderLineId,
                    x.Quantity,
                    x.UnitPriceSnapshot,
                    x.Currency,
                    x.ReservationId,
                    (ReturnItemConditionOption)x.Condition,
                    x.Disposition == ReturnItemDisposition.DamagedNonSaleable
                        ? ReturnItemConditionOption.Damaged
                        : null,
                    x.SourceShipmentId,
                    x.SourceReceiptId))
                .ToList(),
            snapshot.RefundAttempts
                .Select(x => new RefundAttemptSnapshot(
                    x.RefundAttemptId,
                    x.PaymentId,
                    x.Amount,
                    x.Currency,
                    (RefundAttemptOperationStatus)x.Status,
                    x.IdempotencyKey,
                    x.ProviderReference,
                    x.FailureCode,
                    x.CreatedAt,
                    x.CompletedAt))
                .ToList(),
            (ReturnOriginOption)snapshot.Origin,
            snapshot.SourceFulfillmentId,
            snapshot.SourceShipmentId,
            snapshot.SourceReceiptId,
            snapshot.SourceClaimId,
            snapshot.SourceOccurrenceKey,
            snapshot.ClosurePhase.ToString(),
            snapshot.ObligationClosedAt,
            snapshot.StockSettledAt,
            snapshot.ReleasedHeldQuantity);

    private static ReturnEligibilityResult Map(AppModels.ReturnEligibilityResult result) =>
        new(
            result.SellerOrderId,
            result.CheckoutId,
            result.Eligible,
            result.ReasonCode,
            result.EligibleUntil,
            result.LastDeliveredAt,
            result.Lines
                .Select(x => new ReturnLineEligibility(
                    x.OrderLineId,
                    x.DeliveredQuantity,
                    x.AlreadyReturnedQuantity,
                    x.RemainingReturnableQuantity))
                .ToList());
}
