using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>MediatR approve-return use case (single authoritative request shape).</summary>
public sealed record ApproveReturnCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    Guid SellerPartyId,
    RefundDestination? RefundDestination = null) : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — seller scope then directory approve.</summary>
public sealed class ApproveReturnHandler(IReturnDirectory returns)
    : IRequestHandler<ApproveReturnCommand, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(ApproveReturnCommand request, CancellationToken cancellationToken)
    {
        var existing = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (existing is null || existing.SellerPartyId != request.SellerPartyId)
        {
            return Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing));
        }

        return await ReturnsOperation.ExecuteAsync(() => returns.ApproveAsync(request, cancellationToken));
    }
}
