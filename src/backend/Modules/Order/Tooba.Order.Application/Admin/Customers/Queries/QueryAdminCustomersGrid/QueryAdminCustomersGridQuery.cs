using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Customers.Models;
using Tooba.Order.Application.Admin.Customers.Ports;

namespace Tooba.Order.Application.Admin.Customers.Queries.QueryAdminCustomersGrid;

/// <summary>صفحه‌بندی server-side گرید مشتریان مدیر.</summary>
public sealed record QueryAdminCustomersGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminCustomerListItem>>>;

/// <summary>درخواست را با whitelist گرید normalize و به خوانندهٔ DB-native واگذار می‌کند.</summary>
public sealed class QueryAdminCustomersGridHandler(IAdminCustomersGridReader reader)
    : IRequestHandler<QueryAdminCustomersGridQuery, Result<GridPageResponse<AdminCustomerListItem>>>
{
    public async Task<Result<GridPageResponse<AdminCustomerListItem>>> Handle(
        QueryAdminCustomersGridQuery request,
        CancellationToken cancellationToken)
    {
        GridQueryRequest normalized;
        try
        {
            normalized = AdminCustomersGridPolicy.Normalize(request.Request);
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<GridPageResponse<AdminCustomerListItem>>(new SemanticError(ex.ErrorCode));
        }

        return Result.Success(await reader.QueryAsync(normalized, cancellationToken));
    }
}
