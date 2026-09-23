using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.Customers.Queries.ListAdminCustomers;
using Tooba.Order.Application.Admin.Customers.Queries.QueryAdminCustomersGrid;

using Tooba.Order.Endpoints;

namespace Tooba.Order.Endpoints.Admin.Customers;

internal static class AdminCustomersEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/customers", ListAsync);
        app.MapPost("/v1/admin/customers/query", QueryAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new ListAdminCustomersQuery(), ct));
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
        return api.From(await sender.Send(new QueryAdminCustomersGridQuery(body), ct));
    }
}
