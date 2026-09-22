using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;

namespace Tooba.Fulfillment.Application.Queries.ListAdminFulfillments;

public sealed record ListAdminFulfillmentsQuery : IRequest<Result<IReadOnlyList<FulfillmentSnapshot>>>;

public sealed class ListAdminFulfillmentsHandler
    : IRequestHandler<ListAdminFulfillmentsQuery, Result<IReadOnlyList<FulfillmentSnapshot>>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    public ListAdminFulfillmentsHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;
    public async Task<Result<IReadOnlyList<FulfillmentSnapshot>>> Handle(
        ListAdminFulfillmentsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _fulfillment.ListAllAsync(cancellationToken));
}
