using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Domain;

namespace Tooba.Host.Content;

/// <summary>مسیرهای Admin نویسندهٔ مقاله.</summary>
public static class ContentAuthorEndpoints
{
    /// <summary>مسیرهای Admin نویسندهٔ مقاله را ثبت می‌کند.</summary>
    public static void MapContentAuthorEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/content/authors");
        admin.MapPost("/query", QueryGridAsync);
        admin.MapGet("/picker", GetPickerListAsync);
        admin.MapGet("/{id:guid}", GetWorkspaceAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPost("/{id:guid}/deactivate", DeactivateAsync);
    }

    private static async Task<IResult> QueryGridAsync(
        GridQueryRequest body,
        ContentAuthorPanelComposer composer,
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
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.QueryGridAsync(body, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> GetPickerListAsync(
        string? search,
        bool activeOnly,
        ContentAuthorPanelComposer composer,
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
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.GetPickerListAsync(search, activeOnly, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid id,
        ContentAuthorPanelComposer composer,
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
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.View, cancellationToken);
            var workspace = await composer.GetWorkspaceAsync(id, cancellationToken);
            return workspace is null
                ? Results.Json(new { title = "Not Found", errorCode = ContentAuthorErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(workspace);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> CreateAsync(
        CreateContentAuthorHttpRequest body,
        ContentAuthorPanelComposer composer,
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
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Create, cancellationToken);
            var created = await composer.CreateAsync(new CreateContentAuthorCommand(
                body.DisplayName ?? "",
                body.Slug ?? "",
                body.ShortBio,
                body.FullBio,
                body.ProfileImageMediaAssetId,
                body.CoverImageMediaAssetId,
                body.WebsiteUrl,
                body.InstagramUrl,
                body.TwitterUrl,
                body.LinkedInUrl), cancellationToken);
            return Results.Json(created, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateContentAuthorHttpRequest body,
        ContentAuthorPanelComposer composer,
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
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            var updated = await composer.UpdateAsync(id, new UpdateContentAuthorCommand(
                body.DisplayName ?? "",
                body.Slug ?? "",
                body.ShortBio,
                body.FullBio,
                body.ProfileImageMediaAssetId,
                body.CoverImageMediaAssetId,
                body.WebsiteUrl,
                body.InstagramUrl,
                body.TwitterUrl,
                body.LinkedInUrl), cancellationToken);
            return Results.Json(updated);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> DeactivateAsync(
        Guid id,
        ContentAuthorPanelComposer composer,
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
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            await composer.DeactivateAsync(id, cancellationToken);
            return Results.Ok();
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult MapInvalid(InvalidOperationException ex) =>
        Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
}

/// <summary>بدنهٔ ایجاد نویسنده.</summary>
public sealed record CreateContentAuthorHttpRequest(
    string? DisplayName,
    string? Slug,
    string? ShortBio,
    string? FullBio,
    Guid? ProfileImageMediaAssetId,
    Guid? CoverImageMediaAssetId,
    string? WebsiteUrl,
    string? InstagramUrl,
    string? TwitterUrl,
    string? LinkedInUrl);

/// <summary>بدنهٔ به‌روزرسانی نویسنده.</summary>
public sealed record UpdateContentAuthorHttpRequest(
    string? DisplayName,
    string? Slug,
    string? ShortBio,
    string? FullBio,
    Guid? ProfileImageMediaAssetId,
    Guid? CoverImageMediaAssetId,
    string? WebsiteUrl,
    string? InstagramUrl,
    string? TwitterUrl,
    string? LinkedInUrl);
