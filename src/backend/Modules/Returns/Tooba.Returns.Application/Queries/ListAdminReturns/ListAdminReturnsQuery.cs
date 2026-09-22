using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;

namespace Tooba.Returns.Application.Queries.ListAdminReturns;

public sealed record ListAdminReturnsQuery()
    : IRequest<Result<IReadOnlyList<ReturnSnapshot>>>;

public sealed class ListAdminReturnsHandler(IReturnDirectory returns)
    : IRequestHandler<ListAdminReturnsQuery, Result<IReadOnlyList<ReturnSnapshot>>>
{
    public async Task<Result<IReadOnlyList<ReturnSnapshot>>> Handle(
        ListAdminReturnsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await returns.ListAllAsync(cancellationToken));
}
