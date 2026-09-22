using Tooba.BuildingBlocks;
using Tooba.Order.Application.Admin.InventoryRecovery.Services;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.Admin.Supply.Services;

namespace Tooba.Order.Infrastructure.Admin.Operations;

/// <summary>Order-owned adapter over recovery service for Ops orchestrator.</summary>
internal sealed class AdminOrderOperationsInventoryRecoveryAdapter(
    OrderInventoryRecoveryService recovery) : IAdminOrderOperationsInventoryRecoveryPort
{
    public async Task<AdminOrderInventoryRecoveryAssessment> AssessAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var result = await recovery.AssessCheckoutAsync(checkoutId, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            throw new ContractOperationException(result.IsFailure ? result.FirstError.Code : "order.operation.invalid");
        }

        var assessment = result.Value;
        return new AdminOrderInventoryRecoveryAssessment(
            assessment.CheckoutId,
            assessment.ClassCode,
            assessment.NeedsRecovery,
            assessment.ReasonFa);
    }

    public async Task<AdminOrderInventoryRecoveryResult> RecoverAsync(
        Guid checkoutId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var result = await recovery.RecoverAsync(checkoutId, actorUserId, reason, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            throw new ContractOperationException(
                result.IsFailure ? result.FirstError.Code : "inventory.recovery.insufficient");
        }

        var value = result.Value;
        return new AdminOrderInventoryRecoveryResult(
            value.Outcome,
            value.ClassCode,
            value.NeedsRecovery,
            value.MessageFa,
            value.OrderNumbers);
    }
}

/// <summary>Order-owned adapter over supply service for Ops orchestrator.</summary>
internal sealed class AdminOrderOperationsSupplyAdapter(OrderSupplyService supply) : IAdminOrderOperationsSupplyPort
{
    public async Task<AdminOrderOpsSupplyStatus> GetStatusAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var result = await supply.GetStatusAsync(checkoutId, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            throw new ContractOperationException(result.IsFailure ? result.FirstError.Code : "order.operation.invalid");
        }

        return Map(result.Value);
    }

    public async Task<AdminOrderOpsSupplyEnsureResult> EnsurePaidDurableAsync(
        Guid checkoutId,
        string reason,
        CancellationToken cancellationToken)
    {
        var result = await supply.EnsureAsync(
            checkoutId,
            OrderSupplyMode.EnsurePaidDurable,
            allowReacquire: true,
            reason,
            cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            throw new ContractOperationException(result.IsFailure ? result.FirstError.Code : "order.operation.invalid");
        }

        var value = result.Value;
        var unavailable = value.Outcome is OrderSupplyOutcome.Unavailable or OrderSupplyOutcome.PartiallyUnavailable
            || value.Status is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable;
        return new AdminOrderOpsSupplyEnsureResult(
            value.Outcome.ToString(),
            value.Status.ToString(),
            unavailable);
    }

    private static AdminOrderOpsSupplyStatus Map(OrderSupplyStatus status) =>
        new(
            status.CheckoutId,
            status.Status.ToString(),
            OrderSupplyMessages.MessageFa(status.Status),
            status.Lines.Select(x => new AdminOrderOpsSupplyLine(
                x.ItemTitle,
                x.UnitCode,
                x.Required,
                x.Available,
                x.Shortage,
                x.LineStatus.ToString())).ToList());
}
