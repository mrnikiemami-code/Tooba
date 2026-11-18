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
    RefundDestination? RefundDestination = null,
    // TB-279: the EFFECTIVE decision revision the operator reviewed; a newer revision refuses this stale one.
    int? ExpectedDecisionVersion = null,
    // TB-279-R2 (D): the reviewed PLAN revision the decision was actually taken against. Required when the case owns a
    // reviewed plan, so an approval can never bless a plan version the operator did not read.
    int? ExpectedResolutionVersion = null) : IRequest<Result<ReturnSnapshot>>;

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
