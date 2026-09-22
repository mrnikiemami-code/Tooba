using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Application.Queries.GetAdminReturn;

public sealed record GetAdminReturnQuery(Guid ReturnRequestId)
    : IRequest<Result<ReturnSnapshot>>;

public sealed class GetAdminReturnHandler(IReturnDirectory returns)
    : IRequestHandler<GetAdminReturnQuery, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(GetAdminReturnQuery request, CancellationToken cancellationToken)
    {
        var page = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        return page is null
            ? Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing))
            : Result.Success(page);
    }
}
