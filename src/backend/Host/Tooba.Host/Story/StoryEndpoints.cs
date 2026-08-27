using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;
using global::Tooba.Story.Application;
using global::Tooba.Story.Domain;

namespace Tooba.Host.Story;

/// <summary>مرزهای HTTP عمومی، فروشنده و مدیریتی Story.</summary>
public static class StoryEndpoints
{
    /// <summary>مسیرهای Story را ثبت می‌کند.</summary>
    public static void MapStoryEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/storefront/stories", GetPublicStoriesAsync);

        var seller = app.MapGroup("/v1/seller/stories");
        seller.MapGet("", SellerListAsync);
        seller.MapGet("/{id:guid}", SellerGetAsync);
        seller.MapPost("", SellerCreateAsync);
        seller.MapPut("/{id:guid}", SellerUpdateAsync);
        seller.MapPost("/{id:guid}/submit", SellerSubmitAsync);
        seller.MapPost("/{id:guid}/items", SellerAddItemAsync);
        seller.MapPut("/{id:guid}/items/{itemId:guid}", SellerUpdateItemAsync);
        seller.MapDelete("/{id:guid}/items/{itemId:guid}", SellerRemoveItemAsync);
        seller.MapPut("/{id:guid}/items/reorder", SellerReorderItemsAsync);

        var admin = app.MapGroup("/v1/admin/stories");
        admin.MapGet("", AdminListAsync);
        admin.MapGet("/{id:guid}", AdminGetAsync);
        admin.MapPost("", AdminCreateAsync);
        admin.MapPut("/{id:guid}", AdminUpdateAsync);
        admin.MapPut("/reorder", AdminReorderAsync);
        admin.MapPost("/{id:guid}/enable", AdminEnableAsync);
        admin.MapPost("/{id:guid}/disable", AdminDisableAsync);
        admin.MapPost("/{id:guid}/schedule", AdminScheduleAsync);
        admin.MapPost("/{id:guid}/approve", AdminApproveAsync);
        admin.MapPost("/{id:guid}/reject", AdminRejectAsync);
        admin.MapPost("/{id:guid}/items", AdminAddItemAsync);
        admin.MapPut("/{id:guid}/items/{itemId:guid}", AdminUpdateItemAsync);
        admin.MapDelete("/{id:guid}/items/{itemId:guid}", AdminRemoveItemAsync);
        admin.MapPut("/{id:guid}/items/reorder", AdminReorderItemsAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult ToMutationError(InvalidOperationException ex)
    {
        var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
        var unsafeCta = ex.Message.Contains("ناامن", StringComparison.Ordinal);
        var errorCode = missing
            ? "story.missing"
            : unsafeCta
                ? "story.cta.rejected"
                : "story.mutation.rejected";
        var statusCode = missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
        return Results.Json(
            new { title = missing ? "Not Found" : "Bad Request", errorCode, detail = ex.Message },
            statusCode: statusCode);
    }

    private static async Task<IResult> GetPublicStoriesAsync(
        StoryPanelComposer composer,
        ICurrentTenant tenant,
        string? locale = null,
        string? market = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            return Results.Json(await composer.GetPublicStoriesAsync(tenantId, locale, market, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = "story.tenant.missing", detail = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SellerListAsync(
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            return Results.Json(await composer.SellerListAsync(tenantId, sellerPartyId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = "story.tenant.missing", detail = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SellerGetAsync(
        Guid id,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            var story = await composer.SellerGetAsync(tenantId, sellerPartyId, id, cancellationToken);
            return story is null ? Results.NotFound() : Results.Json(story);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerCreateAsync(
        CreateStoryBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, actorUserId, sellerPartyId) =>
                composer.SellerCreateDraftAsync(tenantId, sellerPartyId, actorUserId, body, cancellationToken),
            successStatusCode: StatusCodes.Status201Created);

    private static async Task<IResult> SellerUpdateAsync(
        Guid id,
        UpdateStoryBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _, sellerPartyId) =>
                composer.SellerUpdateAsync(tenantId, sellerPartyId, id, body, cancellationToken));

    private static async Task<IResult> SellerSubmitAsync(
        Guid id,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, actorUserId, sellerPartyId) =>
                composer.SellerSubmitAsync(tenantId, sellerPartyId, id, actorUserId, cancellationToken));

    private static async Task<IResult> SellerAddItemAsync(
        Guid id,
        AddStoryItemBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _, sellerPartyId) =>
                composer.SellerAddItemAsync(tenantId, sellerPartyId, id, body, cancellationToken),
            successStatusCode: StatusCodes.Status201Created);

    private static async Task<IResult> SellerUpdateItemAsync(
        Guid id,
        Guid itemId,
        UpdateStoryItemBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _, sellerPartyId) =>
                composer.SellerUpdateItemAsync(tenantId, sellerPartyId, id, itemId, body, cancellationToken));

    private static async Task<IResult> SellerRemoveItemAsync(
        Guid id,
        Guid itemId,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _, sellerPartyId) =>
                composer.SellerRemoveItemAsync(tenantId, sellerPartyId, id, itemId, cancellationToken));

    private static async Task<IResult> SellerReorderItemsAsync(
        Guid id,
        ReorderStoryItemsBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _, sellerPartyId) =>
                composer.SellerReorderItemsAsync(tenantId, sellerPartyId, id, body.ItemIds, cancellationToken));

    private static async Task<IResult> AdminListAsync(
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? reviewStatus = null,
        bool pendingReview = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            if (pendingReview)
                return Results.Json(await composer.AdminListPendingReviewAsync(tenantId, cancellationToken));

            StoryReviewStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(reviewStatus))
            {
                if (!Enum.TryParse<StoryReviewStatus>(reviewStatus, ignoreCase: true, out var value)
                    || !Enum.IsDefined(value))
                {
                    return Results.Json(
                        new { title = "Bad Request", errorCode = "story.reviewStatus.invalid" },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                parsed = value;
            }

            return Results.Json(await composer.AdminListAsync(tenantId, parsed, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = "story.tenant.missing", detail = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid id,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            var story = await composer.AdminGetAsync(tenantId, id, cancellationToken);
            return story is null ? Results.NotFound() : Results.Json(story);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminCreateAsync(
        CreateStoryBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminCreateAsync(tenantId, body, cancellationToken),
            successStatusCode: StatusCodes.Status201Created);

    private static async Task<IResult> AdminUpdateAsync(
        Guid id,
        UpdateStoryBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminUpdateAsync(tenantId, id, body, cancellationToken));

    private static async Task<IResult> AdminReorderAsync(
        ReorderStoriesBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminReorderStoriesAsync(tenantId, body.StoryIds, cancellationToken));

    private static async Task<IResult> AdminEnableAsync(
        Guid id,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminEnableAsync(tenantId, id, cancellationToken));

    private static async Task<IResult> AdminDisableAsync(
        Guid id,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminDisableAsync(tenantId, id, cancellationToken));

    private static async Task<IResult> AdminScheduleAsync(
        Guid id,
        SetStoryScheduleBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminSetScheduleAsync(tenantId, id, body, cancellationToken));

    private static async Task<IResult> AdminApproveAsync(
        Guid id,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, actorUserId) => composer.AdminApproveAsync(tenantId, id, actorUserId, cancellationToken));

    private static async Task<IResult> AdminRejectAsync(
        Guid id,
        RejectStoryBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, actorUserId) =>
                composer.AdminRejectAsync(tenantId, id, actorUserId, body.Reason ?? string.Empty, cancellationToken));

    private static async Task<IResult> AdminAddItemAsync(
        Guid id,
        AddStoryItemBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminAddItemAsync(tenantId, id, body, cancellationToken),
            successStatusCode: StatusCodes.Status201Created);

    private static async Task<IResult> AdminUpdateItemAsync(
        Guid id,
        Guid itemId,
        UpdateStoryItemBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminUpdateItemAsync(tenantId, id, itemId, body, cancellationToken));

    private static async Task<IResult> AdminRemoveItemAsync(
        Guid id,
        Guid itemId,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminRemoveItemAsync(tenantId, id, itemId, cancellationToken));

    private static async Task<IResult> AdminReorderItemsAsync(
        Guid id,
        ReorderStoryItemsBody body,
        StoryPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            (tenantId, _) => composer.AdminReorderItemsAsync(tenantId, id, body.ItemIds, cancellationToken));

    private static async Task<IResult> SellerMutationAsync<T>(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        Func<Guid, Guid, Guid, Task<T>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            var result = await action(tenantId, actorUserId, sellerPartyId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return ToMutationError(ex); }
    }

    private static async Task<IResult> AdminMutationAsync<T>(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        Func<Guid, Guid, Task<T>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            var actorUserId = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            var result = await action(tenantId, actorUserId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return ToMutationError(ex); }
    }
}

/// <summary>بدنهٔ ایجاد استوری.</summary>
public sealed record CreateStoryBody(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    int? DisplayOrder,
    string? CtaType,
    string? CtaTarget);

/// <summary>بدنهٔ به‌روزرسانی استوری.</summary>
public sealed record UpdateStoryBody(
    string Title,
    string? Locale,
    string? Market,
    Guid? CoverMediaAssetId,
    string? CoverMediaUrl,
    string? CtaType,
    string? CtaTarget);

/// <summary>بدنهٔ زمان‌بندی استوری.</summary>
public sealed record SetStoryScheduleBody(DateTimeOffset? StartAt, DateTimeOffset? EndAt);

/// <summary>بدنهٔ رد استوری.</summary>
public sealed record RejectStoryBody(string? Reason);

/// <summary>بدنهٔ مرتب‌سازی استوری‌ها.</summary>
public sealed record ReorderStoriesBody(IReadOnlyList<Guid> StoryIds);

/// <summary>بدنهٔ افزودن آیتم.</summary>
public sealed record AddStoryItemBody(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget,
    int? DisplayOrder);

/// <summary>بدنهٔ به‌روزرسانی آیتم.</summary>
public sealed record UpdateStoryItemBody(
    string MediaType,
    Guid? MediaAssetId,
    string? MediaUrl,
    string? Caption,
    int? DurationMs,
    string? CtaType,
    string? CtaTarget);

/// <summary>بدنهٔ مرتب‌سازی آیتم‌ها.</summary>
public sealed record ReorderStoryItemsBody(IReadOnlyList<Guid> ItemIds);
