using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Application.Queries.GetCustomerReturn;

public sealed record GetCustomerReturnQuery(Guid CustomerUserId, Guid ReturnRequestId)
    : IRequest<Result<ReturnSnapshot>>;

public sealed class GetCustomerReturnHandler(IReturnDirectory returns)
    : IRequestHandler<GetCustomerReturnQuery, Result<ReturnSnapshot>>
{
    public async Task<Result<ReturnSnapshot>> Handle(GetCustomerReturnQuery request, CancellationToken cancellationToken)
    {
        var page = await returns.GetAsync(request.ReturnRequestId, cancellationToken);
        if (page is null || page.RequestedByUserId != request.CustomerUserId)
        {
            return Result.Failure<ReturnSnapshot>(new SemanticError(ReturnsErrorCodes.Missing));
        }

        return Result.Success(page);
    }
}
