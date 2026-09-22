using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;

namespace Tooba.Returns.Application.Queries.ListCustomerReturns;

public sealed record ListCustomerReturnsQuery(Guid CustomerUserId)
    : IRequest<Result<IReadOnlyList<ReturnSnapshot>>>;

public sealed class ListCustomerReturnsHandler(IReturnDirectory returns)
    : IRequestHandler<ListCustomerReturnsQuery, Result<IReadOnlyList<ReturnSnapshot>>>
{
    public async Task<Result<IReadOnlyList<ReturnSnapshot>>> Handle(
        ListCustomerReturnsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await returns.ListForCustomerAsync(request.CustomerUserId, cancellationToken));
}
