using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;
using Tooba.Support.Application;
using Tooba.Support.Infrastructure;

namespace Tooba.Host.Support;

/// <summary>مرزهای HTTP مشتری، فروشنده و Admin برای تیکت پشتیبانی.</summary>
public static class SupportEndpoints
{
    internal const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای Support را ثبت می‌کند.</summary>
    public static void MapSupportEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/customer/support/tickets", CustomerListAsync);
        app.MapPost("/v1/customer/support/tickets", CustomerCreateAsync);
        app.MapGet("/v1/customer/support/tickets/{ticketId:guid}", CustomerGetAsync);
        app.MapPost("/v1/customer/support/tickets/{ticketId:guid}/replies", CustomerReplyAsync);
        app.MapPost("/v1/customer/support/tickets/{ticketId:guid}/close", CustomerCloseAsync);
        app.MapPost("/v1/customer/support/tickets/{ticketId:guid}/reopen", CustomerReopenAsync);

        app.MapGet("/v1/seller/support/tickets", SellerListAsync);
        app.MapPost("/v1/seller/support/tickets", SellerCreateAsync);
        app.MapGet("/v1/seller/support/tickets/{ticketId:guid}", SellerGetAsync);
        app.MapPost("/v1/seller/support/tickets/{ticketId:guid}/replies", SellerReplyAsync);
        app.MapPost("/v1/seller/support/tickets/{ticketId:guid}/close", SellerCloseAsync);
        app.MapPost("/v1/seller/support/tickets/{ticketId:guid}/reopen", SellerReopenAsync);

        app.MapGet("/v1/admin/support/tickets", AdminListAsync);
        app.MapGet("/v1/admin/support/tickets/{ticketId:guid}", AdminGetAsync);
        app.MapPost("/v1/admin/support/tickets/{ticketId:guid}/replies", AdminReplyAsync);
        app.MapPatch("/v1/admin/support/tickets/{ticketId:guid}", AdminPatchAsync);
        app.MapGet("/v1/admin/support/demo-preview", AdminDemoPreviewAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult Rejected(string errorCode, string? detail = null) =>
        Results.Json(new { title = "Bad Request", errorCode, detail }, statusCode: 400);

    private static IResult Missing() =>
        Results.Json(new { title = "Not Found", errorCode = "support.missing" }, statusCode: 404);

    private sealed record CreateTicketBody(
        string Subject,
        string Category,
        string? Priority,
        string Body,
        string? RelatedEntityType,
        Guid? RelatedEntityId);

    private sealed record ReplyTicketBody(string Body, bool IsInternalNote = false);

    private sealed record AdminPatchBody(
        string? Status,
        string? Priority,
        Guid? AssignedOperatorActorUserId);

    private static async Task<IResult> CustomerListAsync(
        ISupportDirectory support,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
            return Results.Json(new { title = "Unauthorized", errorCode = "customer.session.required" }, statusCode: 401);
        try
        {
            return Results.Json(await support.ListForCustomerAsync(
                actor.Value, new AudienceTicketListQuery(status, page, pageSize), cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return Rejected("support.rejected");
        }
    }

    private static async Task<IResult> CustomerCreateAsync(
        CreateTicketBody body,
        ISupportDirectory support,
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
            var snapshot = await support.CreateForCustomerAsync(
                actor.Value,
                new CreateTicketCommand(
                    body.Subject,
                    body.Category,
                    body.Priority,
                    body.Body,
                    body.RelatedEntityType,
                    body.RelatedEntityId,
                    ReadIdempotencyKey(request)),
                cancellationToken);
            return Results.Json(snapshot, statusCode: 201);
        }
        catch (InvalidOperationException)
        {
            return Rejected("support.rejected");
        }
    }

    private static async Task<IResult> CustomerGetAsync(
        Guid ticketId,
        ISupportDirectory support,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
            return Results.Json(new { title = "Unauthorized", errorCode = "customer.session.required" }, statusCode: 401);
        var snapshot = await support.GetForCustomerAsync(actor.Value, ticketId, cancellationToken);
        return snapshot is null ? Missing() : Results.Json(snapshot);
    }

    private static async Task<IResult> CustomerReplyAsync(
        Guid ticketId,
        ReplyTicketBody body,
        ISupportDirectory support,
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
            return Results.Json(await support.ReplyForCustomerAsync(
                actor.Value,
                ticketId,
                new ReplyTicketCommand(body.Body, false, ReadIdempotencyKey(request)),
                cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return Rejected("support.reply.rejected");
        }
    }

    private static async Task<IResult> CustomerCloseAsync(
        Guid ticketId,
        ISupportDirectory support,
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
            return Results.Json(await support.CloseForCustomerAsync(actor.Value, ticketId, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return Rejected("support.action.rejected");
        }
    }

    private static async Task<IResult> CustomerReopenAsync(
        Guid ticketId,
        ISupportDirectory support,
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
            return Results.Json(await support.ReopenForCustomerAsync(actor.Value, ticketId, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return Rejected("support.action.rejected");
        }
    }

    private static async Task<IResult> SellerListAsync(
        ISupportDirectory support,
        IAccessControlDirectory access,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "support.view", access, cancellationToken);
            return Results.Json(await support.ListForSellerAsync(
                sellerPartyId, new AudienceTicketListQuery(status, page, pageSize), cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.rejected"); }
    }

    private static async Task<IResult> SellerCreateAsync(
        CreateTicketBody body,
        ISupportDirectory support,
        IAccessControlDirectory access,
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
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "support.create", access, cancellationToken);
            var snapshot = await support.CreateForSellerAsync(
                actorUserId,
                sellerPartyId,
                new CreateTicketCommand(
                    body.Subject,
                    body.Category,
                    body.Priority,
                    body.Body,
                    body.RelatedEntityType,
                    body.RelatedEntityId,
                    ReadIdempotencyKey(request)),
                cancellationToken);
            return Results.Json(snapshot, statusCode: 201);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.rejected"); }
    }

    private static async Task<IResult> SellerGetAsync(
        Guid ticketId,
        ISupportDirectory support,
        IAccessControlDirectory access,
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
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "support.view", access, cancellationToken);
            var snapshot = await support.GetForSellerAsync(sellerPartyId, ticketId, cancellationToken);
            return snapshot is null ? Missing() : Results.Json(snapshot);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerReplyAsync(
        Guid ticketId,
        ReplyTicketBody body,
        ISupportDirectory support,
        IAccessControlDirectory access,
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
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "support.reply", access, cancellationToken);
            return Results.Json(await support.ReplyForSellerAsync(
                actorUserId,
                sellerPartyId,
                ticketId,
                new ReplyTicketCommand(body.Body, false, ReadIdempotencyKey(request)),
                cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.reply.rejected"); }
    }

    private static async Task<IResult> SellerCloseAsync(
        Guid ticketId,
        ISupportDirectory support,
        IAccessControlDirectory access,
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
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "support.reply", access, cancellationToken);
            return Results.Json(await support.CloseForSellerAsync(sellerPartyId, ticketId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.action.rejected"); }
    }

    private static async Task<IResult> SellerReopenAsync(
        Guid ticketId,
        ISupportDirectory support,
        IAccessControlDirectory access,
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
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "support.reply", access, cancellationToken);
            return Results.Json(await support.ReopenForSellerAsync(sellerPartyId, ticketId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.action.rejected"); }
    }

    private static async Task<IResult> AdminListAsync(
        ISupportDirectory support,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        string? status = null,
        string? requesterKind = null,
        string? category = null,
        string? priority = null,
        string? q = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await EnsureAdminCapabilityAsync(actor, "support.view", authz, tenant, cancellationToken);
            return Results.Json(await support.ListForAdminAsync(
                new AdminTicketListQuery(status, requesterKind, category, priority, q, page, pageSize),
                cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.rejected"); }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid ticketId,
        ISupportDirectory support,
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
            await EnsureAdminCapabilityAsync(actor, "support.view", authz, tenant, cancellationToken);
            var snapshot = await support.GetForAdminAsync(ticketId, cancellationToken);
            return snapshot is null ? Missing() : Results.Json(snapshot);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminReplyAsync(
        Guid ticketId,
        ReplyTicketBody body,
        ISupportDirectory support,
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
            await EnsureAdminCapabilityAsync(actor, "support.manage", authz, tenant, cancellationToken);
            return Results.Json(await support.ReplyForAdminAsync(
                actor,
                ticketId,
                new ReplyTicketCommand(body.Body, body.IsInternalNote, ReadIdempotencyKey(request)),
                cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.reply.rejected"); }
    }

    private static async Task<IResult> AdminPatchAsync(
        Guid ticketId,
        AdminPatchBody body,
        ISupportDirectory support,
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
            await EnsureAdminCapabilityAsync(actor, "support.manage", authz, tenant, cancellationToken);
            return Results.Json(await support.PatchForAdminAsync(
                ticketId,
                new AdminTicketPatchCommand(body.Status, body.Priority, body.AssignedOperatorActorUserId),
                cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException) { return Rejected("support.patch.rejected"); }
    }

    private static IResult AdminDemoPreviewAsync(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();
        var demo = SupportDemoSnapshotStore.Current;
        return demo is null
            ? Results.Json(new { title = "Support demo seed not ready", errorCode = "support.demo.not_ready" }, statusCode: 503)
            : Results.Json(demo);
    }

    private static async Task EnsureSellerCapabilityAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        string permissionId,
        IAccessControlDirectory access,
        CancellationToken cancellationToken)
    {
        var effective = await access.GetEffectiveAccessAsync(
            actorUserId,
            new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId),
            cancellationToken);
        var allowed = effective.Permissions.Any(p =>
            p.PermissionId == permissionId
            && !p.DeniedByCeiling
            && p.ScopeKind == AccessScopeKind.GlobalWithinOwner);
        if (!allowed)
            throw new PlatformHttpException(403, "مجوز پشتیبانی وجود ندارد.", "seller.authorization.denied");
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
        // پنل Admin قبلاً tenant#view را پاس کرده؛ تا tupleهای capability پایدار شوند fail-open.
        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
            return;
        throw new PlatformHttpException(403, "مجوز پشتیبانی وجود ندارد.", "admin.authorization.denied");
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

    private static string? ReadIdempotencyKey(HttpRequest request) =>
        request.Headers.TryGetValue("Idempotency-Key", out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw.ToString().Trim()
            : null;
}
