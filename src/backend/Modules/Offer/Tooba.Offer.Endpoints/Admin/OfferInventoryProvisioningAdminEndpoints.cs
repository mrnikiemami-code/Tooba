using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Offer.Application.Offers.Commands.EnsureOfferInventoryPosition;
using Tooba.Offer.Application.Offers.Queries.ListEligibleInventoryLocations;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Endpoints.Seller;

namespace Tooba.Offer.Endpoints.Admin;

/// <summary>
/// Thin Offer-owned routes exposing Inventory's active-location read and idempotent position
/// provisioning on both admin and seller prefixes. All work is dispatched through MediatR.
/// </summary>
public static class OfferInventoryProvisioningAdminEndpoints
{
    /// <summary>Maps the admin and seller eligible-location/provisioning routes.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        MapAdmin(app.MapGroup("/v1/admin"));
        MapSeller(app.MapGroup("/v1/seller"));
    }

    private static void MapAdmin(RouteGroupBuilder group)
    {
        group.MapGet("/offers/inventory-locations", ListLocationsForAdminAsync);
        group.MapPost("/offers/{offerId:guid}/inventory/positions", EnsurePositionForAdminAsync);
    }

    private static void MapSeller(RouteGroupBuilder group)
    {
        group.MapGet("/offers/inventory-locations", ListLocationsForSellerAsync);
        group.MapPost("/offers/{offerId:guid}/inventory/positions", EnsurePositionForSellerAsync);
    }

    private static async Task<IResult> ListLocationsForAdminAsync(
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireViewAsync(context, token);
        return api.From(await sender.Send(new ListEligibleInventoryLocationsQuery(), token));
    }

    private static async Task<IResult> EnsurePositionForAdminAsync(
        Guid offerId,
        OfferInventoryPositionProvisionRequest body,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireEditAsync(context, token);
        return api.From(await sender.Send(
            new EnsureOfferInventoryPositionCommand(offerId, body.LocationId), token));
    }

    private static async Task<IResult> ListLocationsForSellerAsync(
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireEditAsync(context, token);
        return api.From(await sender.Send(new ListEligibleInventoryLocationsQuery(), token));
    }

    private static async Task<IResult> EnsurePositionForSellerAsync(
        Guid offerId,
        OfferInventoryPositionProvisionRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireEditAsync(context, token);
        return api.From(await sender.Send(
            new EnsureOfferInventoryPositionCommand(offerId, body.LocationId, sellerId), token));
    }
}
