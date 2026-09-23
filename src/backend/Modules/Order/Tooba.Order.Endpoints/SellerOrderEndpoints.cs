using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Seller.Queries.GetSellerOrderDetail;
using Tooba.Order.Application.Seller.Queries.ListSellerOrders;

namespace Tooba.Order.Endpoints;

/// <summary>Host transport adapter — resolves authorized seller Actor for Order seller routes.</summary>
public interface IOrderSellerAuthorizer
{
    /// <summary>
    /// Returns (ActorUserId, SellerPartyId) after party#view authorization; otherwise SemanticError.
    /// Never trusts body-supplied seller ids.
    /// </summary>
    Task<(Guid ActorUserId, Guid SellerPartyId, SemanticError? Error)> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);
}

internal static class SellerOrderEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/seller");
        group.MapGet("/orders", ListAsync);
        group.MapGet("/orders/{sellerOrderId:guid}", GetAsync);
    }

    private static async Task<IResult> ListAsync(
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

        return api.From(await sender.Send(new ListSellerOrdersQuery(seller, actor), ct));
    }

    private static async Task<IResult> GetAsync(
        Guid sellerOrderId,
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

        return api.From(await sender.Send(new GetSellerOrderDetailQuery(seller, actor, sellerOrderId), ct));
    }
}
