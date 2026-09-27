using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;
using Tooba.Order.Endpoints;

namespace Tooba.Order.Endpoints.Admin.Settings;

/// <summary>Admin reservation-policy settings — Order-owned HTTP via MediatR.</summary>
public static class ReservationPolicyAdminEndpoints
{
    /// <summary>Maps /v1/admin/settings/reservation-policy/* routes.</summary>
    public static void MapReservationPolicyAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/settings/reservation-policy");
        admin.MapGet("/store", GetStoreAsync);
        admin.MapPut("/store", PutStoreAsync);
        admin.MapGet("/categories/{categoryId:guid}", GetCategoryAsync);
        admin.MapPut("/categories/{categoryId:guid}", PutCategoryAsync);
        admin.MapGet("/offers", GetOffersBatchAsync);
        admin.MapGet("/offers/{offerId:guid}", GetOfferAsync);
        admin.MapPut("/offers/{offerId:guid}", PutOfferAsync);
        admin.MapGet("/audit", GetAuditAsync);
    }

    private static async Task<IResult> GetStoreAsync(
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetStoreReservationPolicyQuery(), cancellationToken));
    }

    private static async Task<IResult> PutStoreAsync(
        ReservationPolicyWriteRequest body,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SaveStoreReservationPolicyCommand(
                body.InitialReservationHoldMinutes,
                body.RetryReservationHoldMinutes,
                body.MaxReservationCycles,
                actor),
            cancellationToken));
    }

    private static async Task<IResult> GetCategoryAsync(
        Guid categoryId,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetCategoryReservationPolicyQuery(categoryId), cancellationToken));
    }

    private static async Task<IResult> PutCategoryAsync(
        Guid categoryId,
        ReservationPolicyWriteRequest body,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SaveCategoryReservationPolicyCommand(
                categoryId,
                body.InitialReservationHoldMinutes,
                body.RetryReservationHoldMinutes,
                body.MaxReservationCycles,
                actor),
            cancellationToken));
    }

    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetOfferReservationPolicyQuery(offerId), cancellationToken));
    }

    private static async Task<IResult> GetOffersBatchAsync(
        string? offerIds,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new GetOffersReservationPolicyBatchQuery(ParseIds(offerIds)),
            cancellationToken));
    }

    private static async Task<IResult> PutOfferAsync(
        Guid offerId,
        ReservationPolicyWriteRequest body,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SaveOfferReservationPolicyCommand(
                offerId,
                body.InitialReservationHoldMinutes,
                body.RetryReservationHoldMinutes,
                body.MaxReservationCycles,
                actor),
            cancellationToken));
    }

    private static async Task<IResult> GetAuditAsync(
        int? take,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAdminAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetReservationPolicyAuditQuery(take), cancellationToken));
    }

    private static List<Guid> ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Take(50)
            .ToList();
    }
}
