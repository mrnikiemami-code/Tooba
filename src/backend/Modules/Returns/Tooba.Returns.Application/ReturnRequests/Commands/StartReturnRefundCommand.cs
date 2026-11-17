using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>
/// TB-277: MediatR use case of the FIRST refund of an already APPROVED case.
/// <para>
/// This is a real financial action and it is deliberately NOT the approval: approving a return records a decision
/// only, while this command is the separate, deliberate step that starts the money. It is also neither the
/// unknown-outcome resume (<c>resume_pending_refund</c> repeats one stored pending attempt) nor the failed-attempt
/// retry (<c>retry_refund</c> mints the deterministic child of one reviewed failure).
/// </para>
/// <para>
/// The caller supplies NO amount, payment, currency, customer or destination: every financial fact is read from the
/// stored case, and the attempt identity/key are derived from the case id in the owning directory.
/// </para>
/// </summary>
public sealed record StartReturnRefundCommand(
    Guid ReturnRequestId,
    Guid ActorUserId) : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — the owning directory reads every financial fact from the stored case.</summary>
public sealed class StartReturnRefundHandler(IReturnDirectory returns)
    : IRequestHandler<StartReturnRefundCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(StartReturnRefundCommand request, CancellationToken cancellationToken) =>
        ReturnsOperation.ExecuteAsync(() => returns.StartFirstRefundAsync(request, cancellationToken));
}
