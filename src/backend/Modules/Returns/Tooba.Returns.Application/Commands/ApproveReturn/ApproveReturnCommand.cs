using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Commands.ApproveReturn;

/// <summary>MediatR approve-return use case.</summary>
public sealed record ApproveReturnCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    Guid SellerPartyId,
    RefundDestination? RefundDestination) : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — seller scope then directory approve.</summary>
public sealed class ApproveReturnHandler(IReturnDirectory returns)
    : IRequestHandler<ApproveReturnCommand, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(ApproveReturnCommand request, CancellationToken cancellationToken)
    {
        var existing = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (existing is null || existing.SellerPartyId != request.SellerPartyId)
        {
            return Result.Failure<ReturnSnapshot>(
                new BuildingBlocks.SemanticError(Contracts.Errors.ReturnsErrorCodes.Missing));
        }

        return await ReturnsExceptionMapper.TryAsync(() => returns.ApproveAsync(
            new Models.ApproveReturnCommand(request.ReturnRequestId, request.ActorUserId, request.RefundDestination),
            cancellationToken));
    }
}
