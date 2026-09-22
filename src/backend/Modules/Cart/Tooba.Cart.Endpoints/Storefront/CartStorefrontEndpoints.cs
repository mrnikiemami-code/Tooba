using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Application.Commands.AddCartLine;
using Tooba.Cart.Application.Commands.ChangeCartLineQuantity;
using Tooba.Cart.Application.Commands.CreateGuestCart;
using Tooba.Cart.Application.Commands.MergeCartAfterLogin;
using Tooba.Cart.Application.Commands.RemoveCartLine;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Application.Queries.GetCart;
using Tooba.Cart.Application.Queries.GetCurrentCart;

namespace Tooba.Cart.Endpoints.Storefront;

/// <summary>Thin storefront Cart HTTP routes — success/failure via ApiResponseFactory.</summary>
public static class CartStorefrontEndpoints
{
    /// <summary>Maps Cart routes under the storefront group.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/cart", CreateGuestCartAsync);
        group.MapGet("/cart/current", GetCurrentAuthenticatedCartAsync);
        group.MapGet("/cart/{cartId:guid}", GetCartAsync);
        group.MapPost("/cart/merge", MergeCartAfterLoginAsync);
        group.MapPost("/cart/{cartId:guid}/lines", AddCartLineAsync);
        group.MapPatch("/cart/{cartId:guid}/lines/{lineId:guid}", ChangeCartLineAsync);
        group.MapDelete("/cart/{cartId:guid}/lines/{lineId:guid}", RemoveCartLineAsync);
    }

    private static async Task<IResult> CreateGuestCartAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateGuestCartCommand(), cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> GetCurrentAuthenticatedCartAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentAuthenticatedCartQuery(), cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> GetCartAsync(
        Guid cartId,
        HttpRequest request,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCartQuery(cartId, ReadGuestSecret(request)), cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> MergeCartAfterLoginAsync(
        CartMergeRequest? body,
        HttpRequest request,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MergeCartAfterLoginCommand(body?.CartId, ReadGuestSecret(request)),
            cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> AddCartLineAsync(
        Guid cartId,
        CartAddLineRequest body,
        HttpRequest request,
        int? expectedVersion,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var version = TryReadExpectedVersion(request, expectedVersion);
        if (version.IsFailure)
        {
            return api.From(version);
        }

        var result = await sender.Send(
            new AddCartLineCommand(
                cartId,
                ReadGuestSecret(request),
                version.Value,
                body.OfferId,
                body.Quantity,
                body.MerchandisingCampaignId),
            cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> ChangeCartLineAsync(
        Guid lineId,
        Guid cartId,
        CartChangeLineRequest body,
        HttpRequest request,
        int? expectedVersion,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var version = TryReadExpectedVersion(request, expectedVersion);
        if (version.IsFailure)
        {
            return api.From(version);
        }

        var result = await sender.Send(
            new ChangeCartLineQuantityCommand(
                cartId,
                ReadGuestSecret(request),
                version.Value,
                lineId,
                body.Quantity),
            cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> RemoveCartLineAsync(
        Guid lineId,
        Guid cartId,
        HttpRequest request,
        int? expectedVersion,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var version = TryReadExpectedVersion(request, expectedVersion);
        if (version.IsFailure)
        {
            return api.From(version);
        }

        var result = await sender.Send(
            new RemoveCartLineCommand(
                cartId,
                ReadGuestSecret(request),
                version.Value,
                lineId),
            cancellationToken);
        return api.From(result);
    }

    private static string? ReadGuestSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Tooba-Guest-Secret", out var header))
        {
            var value = header.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return request.Cookies.TryGetValue("tooba_guest_secret", out var cookie) ? cookie : null;
    }

    private static Result<int> TryReadExpectedVersion(HttpRequest request, int? expectedVersion)
    {
        if (expectedVersion is int queryVersion)
        {
            return Result.Success(queryVersion);
        }

        if (request.Headers.TryGetValue("X-Tooba-Cart-Version", out var header)
            && int.TryParse(header, out var parsed))
        {
            return Result.Success(parsed);
        }

        return Result.Failure<int>(new SemanticError(CartErrorCodes.VersionConflict));
    }
}

/// <summary>Wire model for adding a cart line.</summary>
public sealed record CartAddLineRequest(Guid OfferId, decimal Quantity, Guid? MerchandisingCampaignId = null);

/// <summary>Wire model for changing a cart line quantity.</summary>
public sealed record CartChangeLineRequest(decimal Quantity);

/// <summary>Wire model for merge-after-login.</summary>
public sealed record CartMergeRequest(Guid? CartId);
