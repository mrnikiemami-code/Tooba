using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.LegacyList.Queries.ListAdminOrders;
using Tooba.Order.Application.Admin.OrdersGrid.Queries.QueryAdminOrdersGrid;

using Tooba.Order.Endpoints;

namespace Tooba.Order.Endpoints.Admin.OrdersGrid;

internal static class AdminOrdersGridEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/orders", ListAsync);
        app.MapPost("/v1/admin/orders/query", QueryAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new ListAdminOrdersQuery(), ct));
    }

    private static async Task<IResult> QueryAsync(
        GridQueryRequest body,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new QueryAdminOrdersGridQuery(body), ct));
    }
}
