using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Party.Contracts.Ports;

namespace Tooba.Party.Application.Admin.Sellers.Queries;

/// <summary>
/// POST /v1/admin/sellers/query — صفحه‌بندی server-side گرید فروشندگان Admin.
/// Field/operator whitelist در Party.Infrastructure policy؛ FluentValidation فقط envelope.
/// </summary>
public sealed record QueryAdminSellersGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminSellerListItem>>>;

/// <summary>
/// Handler: <see cref="IAdminSellersGridPort"/> + تبدیل <see cref="GridQueryValidationException"/>
/// به <see cref="SemanticError"/> با ErrorCode پایدار (بدون message متنی exception).
/// </summary>
public sealed class QueryAdminSellersGridQueryHandler(IAdminSellersGridPort sellersGrid)
    : IRequestHandler<QueryAdminSellersGridQuery, Result<GridPageResponse<AdminSellerListItem>>>
{
    /// <inheritdoc />
    public async Task<Result<GridPageResponse<AdminSellerListItem>>> Handle(
        QueryAdminSellersGridQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = await sellersGrid.QueryAsync(request.Request, cancellationToken).ConfigureAwait(false);
            return Result.Success(page);
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<GridPageResponse<AdminSellerListItem>>(new SemanticError(ex.ErrorCode));
        }
    }
}
