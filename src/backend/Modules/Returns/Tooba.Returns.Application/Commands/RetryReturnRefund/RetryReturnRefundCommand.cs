using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;

namespace Tooba.Returns.Application.Commands.RetryReturnRefund;

/// <summary>MediatR admin retry-refund use case.</summary>
public sealed record RetryReturnRefundCommand(Guid ReturnRequestId, Guid ActorUserId)
    : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — Result via ReturnsExceptionMapper.</summary>
public sealed class RetryReturnRefundHandler(IReturnDirectory returns)
    : IRequestHandler<RetryReturnRefundCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(RetryReturnRefundCommand request, CancellationToken cancellationToken) =>
        ReturnsExceptionMapper.TryAsync(() => returns.RetryRefundAsync(
            new RetryRefundCommand(request.ReturnRequestId, request.ActorUserId),
            cancellationToken));
}
