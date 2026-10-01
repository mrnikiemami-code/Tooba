using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Security;
using Tooba.Party.Application.Admin.Sellers.Queries;

namespace Tooba.Party.Endpoints.Admin.Sellers;

/// <summary>
/// Admin sellers HTTP — GET list و POST grid query مالک Party via MediatR و ApiResponseFactory.
/// </summary>
public static class PartyAdminSellersEndpoints
{
    /// <summary>مسیرهای فهرست و گرید فروشندگان Admin را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/admin/sellers", ListAsync);
        app.MapPost("/v1/admin/sellers/query", QueryAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        IAdminPanelAccess adminAccess,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAccess.RequireAuthorizedAsync(request, cancellationToken).ConfigureAwait(false);
        return api.From(await sender.Send(new ListAdminSellersQuery(), cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> QueryAsync(
        GridQueryRequest body,
        HttpRequest request,
        IAdminPanelAccess adminAccess,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAccess.RequireAuthorizedAsync(request, cancellationToken).ConfigureAwait(false);
        return api.From(await sender.Send(new QueryAdminSellersGridQuery(body), cancellationToken).ConfigureAwait(false));
    }
}
