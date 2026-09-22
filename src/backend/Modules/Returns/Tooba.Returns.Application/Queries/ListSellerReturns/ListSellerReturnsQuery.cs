using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;

namespace Tooba.Returns.Application.Queries.ListSellerReturns;

public sealed record ListSellerReturnsQuery(Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<ReturnSnapshot>>>;

public sealed class ListSellerReturnsHandler(IReturnDirectory returns)
    : IRequestHandler<ListSellerReturnsQuery, Result<IReadOnlyList<ReturnSnapshot>>>
{
    public async Task<Result<IReadOnlyList<ReturnSnapshot>>> Handle(
        ListSellerReturnsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await returns.ListForSellerAsync(request.SellerPartyId, cancellationToken));
}
