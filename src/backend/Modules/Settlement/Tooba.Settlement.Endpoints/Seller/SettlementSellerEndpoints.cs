using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Settlement.Application.Commands.RequestSellerPayout;
using Tooba.Settlement.Application.Models;
using Tooba.Settlement.Application.Queries.GetSellerSettlementBalance;
using Tooba.Settlement.Application.Queries.ListSellerPayoutRequests;
using Tooba.Settlement.Application.Queries.ListSellerSettlementEntries;
using Tooba.Settlement.Application.Queries.ListSellerSettlementStatements;

namespace Tooba.Settlement.Endpoints.Seller;

/// <summary>Thin seller Settlement HTTP routes — ApiResponseFactory only.</summary>
public static class SettlementSellerEndpoints
{
    /// <summary>Maps seller Settlement routes under <c>/v1/seller</c>.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/settlement/balance", SellerBalanceAsync);
        group.MapGet("/settlement/entries", SellerEntriesAsync);
        group.MapGet("/settlement/statements", SellerStatementsAsync);
        group.MapGet("/settlement/payout-requests", SellerPayoutListAsync);
        group.MapPost("/settlement/payout-requests", SellerRequestPayoutAsync);
    }

    private static async Task<IResult> SellerBalanceAsync(
        ISender sender,
        ISettlementSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetSellerSettlementBalanceQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> SellerEntriesAsync(
        ISender sender,
        ISettlementSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListSellerSettlementEntriesQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> SellerStatementsAsync(
        ISender sender,
        ISettlementSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListSellerSettlementStatementsQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> SellerPayoutListAsync(
        ISender sender,
        ISettlementSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListSellerPayoutRequestsQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> SellerRequestPayoutAsync(
        RequestPayoutBody body,
        ISender sender,
        ISettlementSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var (actorUserId, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new RequestSellerPayoutCommand(sellerPartyId, actorUserId, body.Amount, body.IdempotencyKey),
            cancellationToken));
    }
}
