using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Settlement.Application.Commands.ProcessAdminPayout;
using Tooba.Settlement.Application.Commands.RetryAdminPayout;
using Tooba.Settlement.Application.Queries.ListAdminPayoutQueue;
using Tooba.Settlement.Application.Queries.ListAdminSettlementBalances;
using Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid;

namespace Tooba.Settlement.Endpoints.Admin;

/// <summary>Thin admin Settlement HTTP routes — ApiResponseFactory only.</summary>
public static class SettlementAdminEndpoints
{
    /// <summary>Maps admin Settlement routes under <c>/v1/admin</c>.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/settlement/balances", AdminBalancesAsync);
        group.MapGet("/settlement/payout-queue", AdminPayoutQueueAsync);
        group.MapPost("/settlement/payout-queue/query", AdminQueryPayoutGridAsync);
        group.MapPost("/settlement/payout-requests/{payoutRequestId:guid}/process", AdminProcessPayoutAsync);
        group.MapPost("/settlement/payout-requests/{payoutRequestId:guid}/retry", AdminRetryPayoutAsync);
    }

    private static async Task<IResult> AdminBalancesAsync(
        ISender sender,
        ISettlementAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListAdminSettlementBalancesQuery(), cancellationToken));
    }

    private static async Task<IResult> AdminPayoutQueueAsync(
        ISender sender,
        ISettlementAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListAdminPayoutQueueQuery(), cancellationToken));
    }

    private static async Task<IResult> AdminQueryPayoutGridAsync(
        GridQueryRequest body,
        ISender sender,
        ISettlementAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new QueryAdminPayoutGridQuery(body), cancellationToken));
    }

    private static async Task<IResult> AdminProcessPayoutAsync(
        Guid payoutRequestId,
        ISender sender,
        ISettlementAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new ProcessAdminPayoutCommand(payoutRequestId, actorUserId),
            cancellationToken));
    }

    private static async Task<IResult> AdminRetryPayoutAsync(
        Guid payoutRequestId,
        ISender sender,
        ISettlementAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new RetryAdminPayoutCommand(payoutRequestId, actorUserId),
            cancellationToken));
    }
}
