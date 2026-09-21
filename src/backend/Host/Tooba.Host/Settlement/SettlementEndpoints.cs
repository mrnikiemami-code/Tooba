using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Host.Seller;
using Tooba.Settlement.Application.Commands;
using Tooba.Settlement.Application.Models;
using Tooba.Settlement.Application.Queries;

namespace Tooba.Host.Settlement;

/// <summary>
/// Endpoint-State: HOST_THIN_TRANSPORT — auth + ISender/ApiResponseFactory only.
/// </summary>
public static class SettlementEndpoints
{
    /// <summary>مسیرهای تسویه را ثبت می‌کند.</summary>
    public static void MapSettlementEndpoints(this WebApplication app)
    {
        var seller = app.MapGroup("/v1/seller");
        seller.MapGet("/settlement/balance", SellerBalanceAsync);
        seller.MapGet("/settlement/entries", SellerEntriesAsync);
        seller.MapGet("/settlement/statements", SellerStatementsAsync);
        seller.MapGet("/settlement/payout-requests", SellerPayoutListAsync);
        seller.MapPost("/settlement/payout-requests", SellerRequestPayoutAsync);

        var admin = app.MapGroup("/v1/admin");
        admin.MapGet("/settlement/balances", AdminBalancesAsync);
        admin.MapGet("/settlement/payout-queue", AdminPayoutQueueAsync);
        admin.MapPost("/settlement/payout-queue/query", AdminQueryPayoutGridAsync);
        admin.MapPost("/settlement/payout-requests/{payoutRequestId:guid}/process", AdminProcessPayoutAsync);
        admin.MapPost("/settlement/payout-requests/{payoutRequestId:guid}/retry", AdminRetryPayoutAsync);
    }

    private static async Task<IResult> SellerBalanceAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(await sender.Send(new GetSellerSettlementBalanceQuery(sellerPartyId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> SellerEntriesAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListSellerSettlementEntriesQuery(sellerPartyId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> SellerStatementsAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListSellerSettlementStatementsQuery(sellerPartyId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> SellerPayoutListAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListSellerPayoutRequestsQuery(sellerPartyId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> SellerRequestPayoutAsync(
        RequestPayoutBody body,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(await sender.Send(
                new RequestSellerPayoutCommand(sellerPartyId, actorUserId, body.Amount, body.IdempotencyKey),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> AdminBalancesAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListAdminSettlementBalancesQuery(), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> AdminPayoutQueueAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListAdminPayoutQueueQuery(), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> AdminQueryPayoutGridAsync(
        GridQueryRequest body,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            var normalized = AdminListGridPolicies.Payouts.Normalize(body);
            return api.From(await sender.Send(new QueryAdminPayoutGridQuery(normalized), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> AdminProcessPayoutAsync(
        Guid payoutRequestId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return api.From(await sender.Send(
                new ProcessAdminPayoutCommand(payoutRequestId, actorUserId),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> AdminRetryPayoutAsync(
        Guid payoutRequestId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return api.From(await sender.Send(
                new RetryAdminPayoutCommand(payoutRequestId, actorUserId),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }
}
