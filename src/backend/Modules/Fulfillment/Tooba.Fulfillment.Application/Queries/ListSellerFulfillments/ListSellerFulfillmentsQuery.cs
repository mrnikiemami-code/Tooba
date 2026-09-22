using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;

namespace Tooba.Fulfillment.Application.Queries.ListSellerFulfillments;

/// <summary>فهرست fulfillment فروشنده.</summary>
public sealed record ListSellerFulfillmentsQuery(Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<FulfillmentSnapshot>>>;

/// <summary>Handler فهرست فروشنده.</summary>
public sealed class ListSellerFulfillmentsHandler
    : IRequestHandler<ListSellerFulfillmentsQuery, Result<IReadOnlyList<FulfillmentSnapshot>>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    public ListSellerFulfillmentsHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;
    public async Task<Result<IReadOnlyList<FulfillmentSnapshot>>> Handle(
        ListSellerFulfillmentsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _fulfillment.ListForSellerAsync(request.SellerPartyId, cancellationToken));
}
