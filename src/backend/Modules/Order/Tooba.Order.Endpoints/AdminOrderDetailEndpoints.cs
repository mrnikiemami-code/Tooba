using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.Detail.Queries.GetAdminOrderDetail;

namespace Tooba.Order.Endpoints;

internal static class AdminOrderDetailEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/orders/{checkoutId:guid}", GetAsync);
    }

    private static async Task<IResult> GetAsync(
        Guid checkoutId,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        var viewerUserId = await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new GetAdminOrderDetailQuery(checkoutId, viewerUserId), ct));
    }
}
