using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts.Operations;
using AppModels = Tooba.Returns.Application.Models;
using ReturnStatusOverlayRow = Tooba.Returns.Contracts.Operations.ReturnStatusOverlayRow;

namespace Tooba.Returns.Infrastructure.Adapters;

/// <summary>
/// Contract-facing adapter over <see cref="IReturnDirectory"/> and <see cref="IReturnEligibilityEvaluator"/>
/// so admin order callers never reference Returns Application/Domain types.
/// Expected failures originate as <see cref="Tooba.BuildingBlocks.ContractOperationException"/> at
/// owning Directory; this adapter does not parse Message or promote InvalidOperationException.
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
            new AppModels.CreateReturnCommand(
                command.SellerOrderId,
                command.ActorUserId,
                command.IdempotencyKey,
                command.Reason,
                command.Items.Select(x => new AppModels.ReturnLineCommand(x.OrderLineId, x.Quantity)).ToList()),
            cancellationToken));

    public async Task<ReturnSnapshot> ApproveAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.ApproveAsync(
            new AppModels.ApproveReturnCommand(returnRequestId, actorUserId), cancellationToken));

    public async Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken) =>
        Map(await directory.RejectAsync(
            new AppModels.RejectReturnCommand(returnRequestId, actorUserId, reason), cancellationToken));

    public async Task<ReturnSnapshot> RetryRefundAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Map(await directory.RetryRefundAsync(
            new AppModels.RetryRefundCommand(returnRequestId, actorUserId), cancellationToken));

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
                    x.ReservationId))
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
                .ToList());

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
