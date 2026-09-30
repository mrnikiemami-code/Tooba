using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Wishlist.Application.Commands.AddWishlistItem;
using Tooba.Wishlist.Application.Commands.RemoveWishlistItem;
using Tooba.Wishlist.Application.Queries.GetWishlistMembership;
using Tooba.Wishlist.Application.Queries.ListWishlistPage;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Endpoints.Customer;

/// <summary>مرز HTTP خصوصی Wishlist — فرمان/کوئری فقط از طریق ISender.</summary>
public static class WishlistCustomerEndpoints
{
    /// <summary>مسیرهای list/add/remove/membership را زیر مرز مشتری ثبت می‌کند.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("", ListAsync);
        group.MapPost("/{productId:guid}", AddAsync);
        group.MapDelete("/{productId:guid}", RemoveAsync);
        group.MapPost("/membership", MembershipAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        IWishlistCustomerActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(WishlistErrorCodes.SessionRequired));
        }

        var page = await sender.Send(new ListWishlistPageQuery(actor.Value), cancellationToken);
        return Results.Json(page);
    }

    private static async Task<IResult> AddAsync(
        Guid productId,
        HttpContext httpContext,
        IWishlistCustomerActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(WishlistErrorCodes.SessionRequired));
        }

        var result = await sender.Send(new AddWishlistItemCommand(actor.Value, productId), cancellationToken);
        return Results.Json(result, statusCode: result.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }

    private static async Task<IResult> RemoveAsync(
        Guid productId,
        HttpContext httpContext,
        IWishlistCustomerActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(WishlistErrorCodes.SessionRequired));
        }

        await sender.Send(new RemoveWishlistItemCommand(actor.Value, productId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MembershipAsync(
        WishlistMembershipRequest body,
        HttpContext httpContext,
        IWishlistCustomerActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(WishlistErrorCodes.SessionRequired));
        }

        var productIds = (body.ProductIds ?? Array.Empty<Guid>()).Distinct().Take(500).ToArray();
        var membership = await sender.Send(
            new GetWishlistMembershipQuery(actor.Value, productIds),
            cancellationToken);
        return Results.Json(membership);
    }
}

/// <summary>درخواست گروهی عضویت که هیچ شناسهٔ مالک دریافت نمی‌کند.</summary>
public sealed record WishlistMembershipRequest(IReadOnlyList<Guid>? ProductIds);
