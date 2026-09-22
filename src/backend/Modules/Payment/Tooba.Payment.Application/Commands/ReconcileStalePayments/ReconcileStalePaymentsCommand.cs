using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Application.Commands.ReconcileStalePayments;

/// <summary>MediatR stale payment reconciliation cycle.</summary>
public sealed record ReconcileStalePaymentsCommand(TimeSpan PendingAge, int BatchSize)
    : IRequest<Result<int>>;

/// <summary>Reconciles stale pending payments using IClock.</summary>
public sealed class ReconcileStalePaymentsHandler(
    IPaymentReconciliationDirectory reconciliation,
    IClock clock)
    : IRequestHandler<ReconcileStalePaymentsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ReconcileStalePaymentsCommand request, CancellationToken cancellationToken)
    {
        var processed = await reconciliation.ReconcileStalePendingAsync(
            clock.UtcNow,
            request.PendingAge,
            request.BatchSize,
            cancellationToken);
        return Result.Success(processed);
    }
}
