using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Queries.GetSellerFulfillment;

public sealed record GetSellerFulfillmentQuery(Guid SellerPartyId, Guid FulfillmentId)
    : IRequest<Result<FulfillmentSnapshot>>;

public sealed class GetSellerFulfillmentHandler
    : IRequestHandler<GetSellerFulfillmentQuery, Result<FulfillmentSnapshot>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    public GetSellerFulfillmentHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;
    public async Task<Result<FulfillmentSnapshot>> Handle(GetSellerFulfillmentQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetAsync(request.FulfillmentId, cancellationToken);
        if (snapshot is null || snapshot.SellerPartyId != request.SellerPartyId)
            return Result.Failure<FulfillmentSnapshot>(new SemanticError(FulfillmentErrorCodes.Missing));
        return Result.Success(snapshot);
    }
}
