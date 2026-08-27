using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Wallet.Application;
using Tooba.Wallet.Infrastructure;

namespace Tooba.Host.Wallet;

/// <summary>مرزهای HTTP مشتری و Admin برای کیف پول و کارت هدیه.</summary>
public static class WalletEndpoints
{
    internal const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای Wallet/GiftCard را ثبت می‌کند.</summary>
    public static void MapWalletEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/customer/wallet", CustomerSummaryAsync);
        app.MapGet("/v1/customer/wallet/ledger", CustomerLedgerAsync);
        app.MapPost("/v1/customer/wallet/gift-cards/redeem", CustomerRedeemAsync);

        app.MapGet("/v1/admin/gift-cards", AdminListGiftCardsAsync);
        app.MapPost("/v1/admin/gift-cards", AdminIssueGiftCardAsync);
        app.MapGet("/v1/admin/gift-cards/{cardId:guid}", AdminGetGiftCardAsync);
        app.MapPost("/v1/admin/gift-cards/{cardId:guid}/revoke", AdminRevokeGiftCardAsync);
        app.MapGet("/v1/admin/wallets/{customerActorUserId:guid}", AdminGetWalletAsync);
        app.MapGet("/v1/admin/wallets/{customerActorUserId:guid}/ledger", AdminWalletLedgerAsync);
        app.MapPost("/v1/admin/wallets/{customerActorUserId:guid}/adjustments", AdminAdjustWalletAsync);
        app.MapGet("/v1/admin/wallet/demo-preview", AdminDemoPreviewAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult Rejected(string errorCode, string? detail = null) =>
        Results.Json(new { title = "Bad Request", errorCode, detail }, statusCode: 400);

    private sealed record RedeemBody(string Code, string? IdempotencyKey);
    private sealed record IssueBody(decimal InitialAmount, string? Currency, DateTimeOffset? ExpiresAt, Guid? RecipientActorUserId, string? IdempotencyKey);
    private sealed record AdjustBody(decimal Amount, string Direction, string Reason, string? IdempotencyKey);

    private static async Task<IResult> CustomerSummaryAsync(
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
            return Results.Json(new { title = "Unauthorized", errorCode = "customer.session.required" }, statusCode: 401);
        try
        {
            return Results.Json(await wallets.GetOrCreateSummaryForCustomerAsync(actor.Value, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Rejected("wallet.rejected", ex.Message);
        }
    }

    private static async Task<IResult> CustomerLedgerAsync(
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
            return Results.Json(new { title = "Unauthorized", errorCode = "customer.session.required" }, statusCode: 401);
        try
        {
            return Results.Json(await wallets.ListLedgerForCustomerAsync(actor.Value, page, pageSize, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Rejected("wallet.rejected", ex.Message);
        }
    }

    private static async Task<IResult> CustomerRedeemAsync(
        RedeemBody body,
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
            return Results.Json(new { title = "Unauthorized", errorCode = "customer.session.required" }, statusCode: 401);
        var idem = body.IdempotencyKey
            ?? request.Headers["Idempotency-Key"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        try
        {
            return Results.Json(await wallets.RedeemGiftCardForCustomerAsync(
                actor.Value, new RedeemGiftCardCommand(body.Code, idem), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Rejected("wallet.redeem.rejected", ex.Message);
        }
    }

    private static async Task<IResult> AdminListGiftCardsAsync(
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        string? status = null,
        string? q = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "giftcard.view", authz, tenant, cancellationToken);
            return Results.Json(await wallets.ListGiftCardsForAdminAsync(
                new AdminGiftCardListQuery(status, q, page, pageSize), cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Rejected("giftcard.rejected", ex.Message); }
    }

    private static async Task<IResult> AdminIssueGiftCardAsync(
        IssueBody body,
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "giftcard.manage", authz, tenant, cancellationToken);
            var idem = body.IdempotencyKey
                ?? request.Headers["Idempotency-Key"].FirstOrDefault()
                ?? Guid.NewGuid().ToString("N");
            return Results.Json(await wallets.IssueGiftCardForAdminAsync(
                actor,
                new IssueGiftCardCommand(body.InitialAmount, body.Currency, body.ExpiresAt, body.RecipientActorUserId, idem),
                cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Rejected("giftcard.issue.rejected", ex.Message); }
    }

    private static async Task<IResult> AdminGetGiftCardAsync(
        Guid cardId,
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "giftcard.view", authz, tenant, cancellationToken);
            var detail = await wallets.GetGiftCardForAdminAsync(cardId, cancellationToken);
            return detail is null
                ? Results.Json(new { title = "Not Found", errorCode = "giftcard.missing" }, statusCode: 404)
                : Results.Json(detail);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminRevokeGiftCardAsync(
        Guid cardId,
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "giftcard.manage", authz, tenant, cancellationToken);
            return Results.Json(await wallets.RevokeGiftCardForAdminAsync(cardId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Rejected("giftcard.revoke.rejected", ex.Message); }
    }

    private static async Task<IResult> AdminGetWalletAsync(
        Guid customerActorUserId,
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "wallet.view", authz, tenant, cancellationToken);
            var summary = await wallets.GetWalletForAdminAsync(customerActorUserId, cancellationToken);
            return summary is null
                ? Results.Json(new { title = "Not Found", errorCode = "wallet.missing" }, statusCode: 404)
                : Results.Json(summary);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminWalletLedgerAsync(
        Guid customerActorUserId,
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "wallet.view", authz, tenant, cancellationToken);
            return Results.Json(await wallets.ListLedgerForAdminAsync(customerActorUserId, page, pageSize, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Rejected("wallet.rejected", ex.Message); }
    }

    private static async Task<IResult> AdminAdjustWalletAsync(
        Guid customerActorUserId,
        AdjustBody body,
        IWalletDirectory wallets,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "wallet.adjust", authz, tenant, cancellationToken);
            var idem = body.IdempotencyKey
                ?? request.Headers["Idempotency-Key"].FirstOrDefault()
                ?? Guid.NewGuid().ToString("N");
            return Results.Json(await wallets.AdjustWalletForAdminAsync(
                customerActorUserId,
                actor,
                new AdminWalletAdjustmentCommand(body.Amount, body.Direction, body.Reason, idem),
                cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Rejected("wallet.adjust.rejected", ex.Message); }
    }

    private static IResult AdminDemoPreviewAsync(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        var demo = WalletDemoSnapshotStore.Current;
        return demo is null
            ? Results.Json(new { title = "Wallet demo seed not ready", errorCode = "wallet.demo.not_ready" }, statusCode: 503)
            : Results.Json(demo);
    }

    private static async Task EnsureAdminCapabilityAsync(
        Guid actorUserId,
        string permissionId,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var decision = await authz.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = permissionId,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = new AuthorizationCallContext
                {
                    Edition = ToobaEdition.SingleStore,
                    TenantId = tenant.Current?.TenantId.Value ?? "unknown",
                },
            },
            cancellationToken);
        if (decision.Kind == AuthorizationDecisionKind.Allow)
            return;
        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
            return;
        throw new PlatformHttpException(403, "مجوز کیف پول/کارت هدیه وجود ندارد.", "admin.authorization.denied");
    }

    private static Guid? ResolveCustomerActor(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } userId) return userId;
        if (environment.IsDevelopment()
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var actor)
            && actor != Guid.Empty)
            return actor;
        return null;
    }
}
