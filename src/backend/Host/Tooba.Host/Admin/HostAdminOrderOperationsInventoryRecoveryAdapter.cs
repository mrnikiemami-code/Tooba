using Tooba.Order.Application.Admin.Operations.Ports;

namespace Tooba.Host.Admin;

/// <summary>
/// Thin Host adapter over OrderInventoryRecoveryComposer — no Order business logic.
/// </summary>
internal sealed class HostAdminOrderOperationsInventoryRecoveryAdapter(
    OrderInventoryRecoveryComposer recovery) : IAdminOrderOperationsInventoryRecoveryPort
{
    public async Task<AdminOrderInventoryRecoveryAssessment> AssessAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var assessment = await recovery.AssessCheckoutAsync(checkoutId, cancellationToken);
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
        return new AdminOrderInventoryRecoveryResult(
            result.Outcome,
            result.ClassCode,
            result.NeedsRecovery,
            result.MessageFa,
            result.OrderNumbers);
    }
}
