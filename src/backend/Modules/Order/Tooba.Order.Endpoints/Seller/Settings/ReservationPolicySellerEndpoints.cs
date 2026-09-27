using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;
using Tooba.Order.Endpoints.Seller;

namespace Tooba.Order.Endpoints.Seller.Settings;

/// <summary>Seller reservation-policy settings — Order-owned HTTP via MediatR.</summary>
public static class ReservationPolicySellerEndpoints
{
    /// <summary>Maps /v1/seller/settings/reservation-policy/* routes.</summary>
    public static void MapReservationPolicySellerEndpoints(this IEndpointRouteBuilder app)
    {
        var seller = app.MapGroup("/v1/seller/settings/reservation-policy");
        seller.MapGet("/offers/{offerId:guid}", GetOfferAsync);
        seller.MapPut("/offers/{offerId:guid}", PutOfferAsync);
    }

    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        ISender sender,
        IOrderSellerAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var (_, _, error) = await auth.ResolveAsync(http, cancellationToken);
        if (error is not null)
        {
            return api.FromFailure(error);
        }

        return api.From(await sender.Send(new GetSellerOfferReservationPolicyQuery(offerId), cancellationToken));
    }

    private static async Task<IResult> PutOfferAsync(
        Guid offerId,
        ReservationPolicyWriteRequest body,
        ISender sender,
        IOrderSellerAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var (_, _, error) = await auth.ResolveAsync(http, cancellationToken);
        if (error is not null)
        {
            return api.FromFailure(error);
        }

        _ = body;
        return api.From(await sender.Send(new DenySellerOfferReservationPolicyCommand(offerId), cancellationToken));
    }
}
