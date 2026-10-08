using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Application.ReturnRequests.Queries;

public sealed record ListCustomerReturnsQuery(Guid CustomerUserId)
    : IRequest<Result<IReadOnlyList<ReturnSnapshot>>>;

public sealed class ListCustomerReturnsHandler(IReturnDirectory returns)
    : IRequestHandler<ListCustomerReturnsQuery, Result<IReadOnlyList<ReturnSnapshot>>>
{
    public async Task<Result<IReadOnlyList<ReturnSnapshot>>> Handle(
        ListCustomerReturnsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await returns.ListForCustomerAsync(request.CustomerUserId, cancellationToken));
}
