using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Seller.Queries.GetSellerOrderDashboardSummary;

namespace Tooba.Order.Endpoints.Seller;

/// <summary>Seller dashboard read — Order-owned HTTP via MediatR (metrics) + Party.Contracts display name.</summary>
internal static class SellerDashboardEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/seller");
        group.MapGet("/dashboard", GetAsync);
    }

    private static async Task<IResult> GetAsync(
        ISender sender,
        IOrderSellerAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        var (actor, seller, error) = await auth.ResolveAsync(context, ct);
        if (error is not null)
        {
            return api.FromFailure(error);
        }

        return api.From(await sender.Send(
            new GetSellerOrderDashboardSummaryQuery(seller, actor), ct));
    }
}
