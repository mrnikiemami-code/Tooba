using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>
/// TB-273: the explicit admin recovery intent of ONE already-approved case.
/// <para>
/// It is deliberately narrow: <see cref="ReturnRecoveryKind.ResumePendingRefund"/> repeats the EXISTING bound
/// pending attempt with its recorded idempotency key/payment/amount/currency, and
/// <see cref="ReturnRecoveryKind.SettlePhysicalDisposition"/> performs the physical phase only. Neither kind
/// mints a new attempt, chooses a destination, recomputes an amount or approves a case. A definite financial
/// failure is a different intent (<c>retry_refund</c>) and is refused here.
/// </para>
/// </summary>
public sealed record RecoverReturnCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    ReturnRecoveryKind Kind,
    Guid? ExpectedRefundAttemptId = null)
    : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — Result via the canonical typed-fault seam.</summary>
public sealed class RecoverReturnHandler(IReturnDirectory returns)
    : IRequestHandler<RecoverReturnCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(RecoverReturnCommand request, CancellationToken cancellationToken) =>
        ReturnsOperation.ExecuteAsync(() => returns.RecoverAsync(request, cancellationToken));
}
