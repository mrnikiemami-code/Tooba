using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Application.Commands.RejectReturn;

/// <summary>MediatR reject-return use case.</summary>
public sealed record RejectReturnCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    Guid SellerPartyId,
    string? Reason) : IRequest<Result<ReturnSnapshot>>;

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

        return await ReturnsExceptionMapper.TryAsync(() => returns.RejectAsync(
            new Models.RejectReturnCommand(request.ReturnRequestId, request.ActorUserId, request.Reason),
            cancellationToken));
    }
}
