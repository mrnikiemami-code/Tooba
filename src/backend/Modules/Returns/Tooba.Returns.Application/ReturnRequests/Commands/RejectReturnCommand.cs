using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>MediatR reject-return use case (single authoritative request shape).</summary>
public sealed record RejectReturnCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    Guid SellerPartyId,
    string? Reason = null) : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — seller scope then directory reject.</summary>
public sealed class RejectReturnHandler(IReturnDirectory returns)
    : IRequestHandler<RejectReturnCommand, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(RejectReturnCommand request, CancellationToken cancellationToken)
    {
        var existing = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (existing is null || existing.SellerPartyId != request.SellerPartyId)
        {
            return Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing));
        }

        return await ReturnsOperation.ExecuteAsync(() => returns.RejectAsync(request, cancellationToken));
    }
}
