using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>MediatR admin retry-refund use case (single authoritative request shape).</summary>
public sealed record RetryReturnRefundCommand(Guid ReturnRequestId, Guid ActorUserId)
    : IRequest<Result<ReturnSnapshot>>
{
    /// <summary>
    /// TB-275 (C2): the exact stored FAILED attempt the operator reviewed. When supplied, the retry only proceeds for
    /// that attempt (a different stored failure is a typed mismatch and mints nothing); when absent, the case's single
    /// coherent bound failure is used, exactly as the recovery read describes it. The child attempt identity is derived
    /// from that reviewed failure in the owning directory, so a duplicate/lost-response POST cannot mint a second one.
    /// </summary>
    public Guid? ExpectedFailedAttemptId { get; init; }

    /// <summary>
    /// TB-279-R1 (F4): the EFFECTIVE decision revision the operator reviewed. The retry mints NEW money, so it is fenced
    /// exactly like the first refund: a newer decision/plan revision refuses this stale review before any reservation.
    /// </summary>
    public int? ExpectedDecisionVersion { get; init; }
}

/// <summary>Handler — Result via the canonical typed-fault seam.</summary>
public sealed class RetryReturnRefundHandler(IReturnDirectory returns)
    : IRequestHandler<RetryReturnRefundCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(RetryReturnRefundCommand request, CancellationToken cancellationToken) =>
        ReturnsOperation.ExecuteAsync(() => returns.RetryRefundAsync(request, cancellationToken));
}
