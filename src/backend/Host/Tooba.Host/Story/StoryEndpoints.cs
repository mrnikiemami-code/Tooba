using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using global::Tooba.Story.Application;

namespace Tooba.Host.Story;

/// <summary>مرزهای HTTP عمومی و مدیریتی Story.</summary>
public static class StoryEndpoints
{
    /// <summary>مسیرهای Story را ثبت می‌کند.</summary>
    public static void MapStoryEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/storefront/stories", GetPublicStoriesAsync);

        var admin = app.MapGroup("/v1/admin/stories");
        admin.MapGet("", AdminListAsync);
        admin.MapGet("/{id:guid}", AdminGetAsync);
        admin.MapPost("", AdminCreateAsync);
        admin.MapPut("/{id:guid}", AdminUpdateAsync);
        admin.MapPut("/reorder", AdminReorderAsync);
        admin.MapPost("/{id:guid}/enable", AdminEnableAsync);
        admin.MapPost("/{id:guid}/disable", AdminDisableAsync);
        admin.MapPost("/{id:guid}/schedule", AdminScheduleAsync);
        admin.MapPost("/{id:guid}/items", AdminAddItemAsync);
        admin.MapPut("/{id:guid}/items/{itemId:guid}", AdminUpdateItemAsync);
        admin.MapDelete("/{id:guid}/items/{itemId:guid}", AdminRemoveItemAsync);
        admin.MapPut("/{id:guid}/items/reorder", AdminReorderItemsAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

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

    private static async Task<IResult> AdminListAsync(
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
            return Results.Json(await composer.AdminListAsync(tenantId, cancellationToken));
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
            tenantId => composer.AdminCreateAsync(tenantId, body, cancellationToken),
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
            tenantId => composer.AdminUpdateAsync(tenantId, id, body, cancellationToken));

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
            tenantId => composer.AdminReorderStoriesAsync(tenantId, body.StoryIds, cancellationToken));

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
            tenantId => composer.AdminEnableAsync(tenantId, id, cancellationToken));

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
            tenantId => composer.AdminDisableAsync(tenantId, id, cancellationToken));

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
            tenantId => composer.AdminSetScheduleAsync(tenantId, id, body, cancellationToken));

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
            tenantId => composer.AdminAddItemAsync(tenantId, id, body, cancellationToken),
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
            tenantId => composer.AdminUpdateItemAsync(tenantId, id, itemId, body, cancellationToken));

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
            tenantId => composer.AdminRemoveItemAsync(tenantId, id, itemId, cancellationToken));

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
            tenantId => composer.AdminReorderItemsAsync(tenantId, id, body.ItemIds, cancellationToken));

    private static async Task<IResult> AdminMutationAsync<T>(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        Func<Guid, Task<T>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var tenantId = StoryPanelComposer.RequireTenantId(tenant);
            var result = await action(tenantId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
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
