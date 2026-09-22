using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Queries.GetAdminFulfillment;

public sealed record GetAdminFulfillmentQuery(Guid FulfillmentId) : IRequest<Result<FulfillmentSnapshot>>;

public sealed class GetAdminFulfillmentHandler
    : IRequestHandler<GetAdminFulfillmentQuery, Result<FulfillmentSnapshot>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    public GetAdminFulfillmentHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;
    public async Task<Result<FulfillmentSnapshot>> Handle(GetAdminFulfillmentQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetAsync(request.FulfillmentId, cancellationToken);
        return snapshot is null
            ? Result.Failure<FulfillmentSnapshot>(new SemanticError(FulfillmentErrorCodes.Missing))
            : Result.Success(snapshot);
    }
}
