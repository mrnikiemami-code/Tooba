using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Application.Queries.GetSellerReturn;

public sealed record GetSellerReturnQuery(Guid SellerPartyId, Guid ReturnRequestId)
    : IRequest<Result<ReturnSnapshot>>;

public sealed class GetSellerReturnHandler(IReturnDirectory returns)
    : IRequestHandler<GetSellerReturnQuery, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(GetSellerReturnQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (snapshot is null || snapshot.SellerPartyId != request.SellerPartyId)
        {
            return Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing));
        }

        return Result.Success(snapshot);
    }
}
