using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Models;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid;

/// <summary>گرید DB-native صف payout (admin).</summary>
public sealed record QueryAdminPayoutGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminPayoutListItem>>>;

/// <summary>Handler گرید payout — module-owned Normalize then Infrastructure engine.</summary>
public sealed class QueryAdminPayoutGridQueryHandler(IAdminPayoutGridQuery grid)
    : IRequestHandler<QueryAdminPayoutGridQuery, Result<GridPageResponse<AdminPayoutListItem>>>
{
    /// <inheritdoc />
    public async Task<Result<GridPageResponse<AdminPayoutListItem>>> Handle(
        QueryAdminPayoutGridQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalized = AdminPayoutGridQueryPolicy.Instance.Normalize(request.Request);
            return Result.Success(await grid.QueryAsync(normalized, cancellationToken));
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<GridPageResponse<AdminPayoutListItem>>(new SemanticError(ex.ErrorCode));
        }
    }
}
