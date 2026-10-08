using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>MediatR admin retry-refund use case (single authoritative request shape).</summary>
public sealed record RetryReturnRefundCommand(Guid ReturnRequestId, Guid ActorUserId)
    : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — Result via the canonical typed-fault seam.</summary>
public sealed class RetryReturnRefundHandler(IReturnDirectory returns)
    : IRequestHandler<RetryReturnRefundCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(RetryReturnRefundCommand request, CancellationToken cancellationToken) =>
        ReturnsOperation.ExecuteAsync(() => returns.RetryRefundAsync(request, cancellationToken));
}
