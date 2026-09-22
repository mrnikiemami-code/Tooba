using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Order.Application.Admin.OrdersGrid.Ports;

namespace Tooba.Order.Application.Admin.OrdersGrid.Queries.QueryAdminOrdersGrid;

/// <summary>صفحه‌بندی server-side گرید سفارش‌های مدیر.</summary>
public sealed record QueryAdminOrdersGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminOrderListItem>>>;

/// <summary>درخواست را با whitelist گرید normalize و به خوانندهٔ DB-native واگذار می‌کند.</summary>
public sealed class QueryAdminOrdersGridHandler(IAdminOrdersGridReader reader)
    : IRequestHandler<QueryAdminOrdersGridQuery, Result<GridPageResponse<AdminOrderListItem>>>
{
    public async Task<Result<GridPageResponse<AdminOrderListItem>>> Handle(
        QueryAdminOrdersGridQuery request,
        CancellationToken cancellationToken)
    {
        GridQueryRequest normalized;
        try
        {
            normalized = AdminOrdersGridPolicy.Normalize(request.Request);
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<GridPageResponse<AdminOrderListItem>>(new SemanticError(ex.ErrorCode));
        }

        return Result.Success(await reader.QueryAsync(normalized, cancellationToken));
    }
}
