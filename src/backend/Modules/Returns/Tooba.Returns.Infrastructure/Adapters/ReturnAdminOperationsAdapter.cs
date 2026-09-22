using Tooba.BuildingBlocks;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts.Operations;
using AppModels = Tooba.Returns.Application.Models;
using ReturnStatusOverlayRow = Tooba.Returns.Contracts.Operations.ReturnStatusOverlayRow;

namespace Tooba.Returns.Infrastructure.Adapters;

/// <summary>
/// Contract-facing adapter over <see cref="IReturnDirectory"/> and <see cref="IReturnEligibilityEvaluator"/>
/// so admin order callers never reference Returns Application/Domain types.
/// Expected failures cross the boundary as <see cref="ContractOperationException"/> (stable Code).
/// </summary>
internal sealed class ReturnAdminOperationsAdapter(
    IReturnDirectory directory,
    IReturnEligibilityEvaluator eligibility) : IReturnAdminOperations
{
    public Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken) =>
        GuardAsync(async () =>
        {
            IReadOnlyList<ReturnSnapshot> list =
                (await directory.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken)).Select(Map).ToList();
            return list;
        });

    public Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(CancellationToken cancellationToken) =>
        GuardAsync(async () =>
        {
            IReadOnlyList<ReturnStatusOverlayRow> list =
                (await directory.ListStatusOverlayAsync(cancellationToken))
                .Select(x => new ReturnStatusOverlayRow(x.SellerOrderId, (ReturnRequestOperationStatus)x.Status))
                .ToList();
            return list;
        });

    public Task<ReturnEligibilityResult> EvaluateEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken) =>
        GuardAsync(async () => Map(await eligibility.EvaluateAsync(sellerOrderId, cancellationToken)));

    public Task<ReturnSnapshot> CreateAdminInitiatedAsync(
        CreateAdminReturnCommand command,
        CancellationToken cancellationToken) =>
        GuardAsync(async () =>
            Map(await directory.CreateAdminInitiatedAsync(
                new AppModels.CreateReturnCommand(
                    command.SellerOrderId,
                    command.ActorUserId,
                    command.IdempotencyKey,
                    command.Reason,
                    command.Items.Select(x => new AppModels.ReturnLineCommand(x.OrderLineId, x.Quantity)).ToList()),
                cancellationToken)));

    public Task<ReturnSnapshot> ApproveAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        GuardAsync(async () =>
            Map(await directory.ApproveAsync(
                new AppModels.ApproveReturnCommand(returnRequestId, actorUserId), cancellationToken)));

    public Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken) =>
        GuardAsync(async () =>
            Map(await directory.RejectAsync(
                new AppModels.RejectReturnCommand(returnRequestId, actorUserId, reason), cancellationToken)));

    public Task<ReturnSnapshot> RetryRefundAsync(
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        GuardAsync(async () =>
            Map(await directory.RetryRefundAsync(
                new AppModels.RetryRefundCommand(returnRequestId, actorUserId), cancellationToken)));

    private static async Task<T> GuardAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (TryPromote(ex, out var fault))
        {
            throw fault;
        }
    }

    private static bool TryPromote(InvalidOperationException ex, out ContractOperationException fault)
    {
        if (ReturnsExceptionMapper.TryMapExact(ex.Message, out var error))
        {
            fault = new ContractOperationException(error.Code, ex);
            return true;
        }

        if (ContractOperationFault.LooksLikeStableCode(ex.Message))
        {
            fault = new ContractOperationException(ex.Message, ex);
            return true;
        }

        fault = null!;
        return false;
    }

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
