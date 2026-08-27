using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.PageComposition.Application;

namespace Tooba.Host.PageComposition;

/// <summary>مرزهای HTTP عمومی و مدیریتی Page Composition.</summary>
public static class PageCompositionEndpoints
{
    /// <summary>مسیرهای Page Composition را ثبت می‌کند.</summary>
    public static void MapPageCompositionEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/storefront/home/composition", GetHomeCompositionAsync);

        var admin = app.MapGroup("/v1/admin/page-composition/home");
        admin.MapGet("", AdminGetHomeAsync);
        admin.MapGet("/catalog", AdminGetCatalogAsync);
        admin.MapPut("/reorder", AdminReorderAsync);
        admin.MapPut("/sections/{id:guid}", AdminUpdateSectionAsync);
        admin.MapPost("/sections", AdminAddSectionAsync);
        admin.MapDelete("/sections/{id:guid}", AdminRemoveSectionAsync);
        admin.MapPost("/restore-default", AdminRestoreDefaultAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> GetHomeCompositionAsync(
        PageCompositionPanelComposer composer,
        ICurrentTenant tenant,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = PageCompositionPanelComposer.RequireTenantId(tenant);
            return Results.Json(await composer.GetHomeCompositionAsync(tenantId, locale, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = "page-composition.tenant.missing", detail = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminGetCatalogAsync(
        PageCompositionPanelComposer composer,
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
            return Results.Json(await composer.GetCatalogAsync(cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminGetHomeAsync(
        PageCompositionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var tenantId = PageCompositionPanelComposer.RequireTenantId(tenant);
            return Results.Json(await composer.AdminGetHomeAsync(tenantId, locale, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = "page-composition.tenant.missing", detail = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminReorderAsync(
        ReorderHomeSectionsBody body,
        PageCompositionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            locale,
            tenantId => composer.AdminReorderHomeAsync(tenantId, locale, body.SectionIds, cancellationToken));

    private static async Task<IResult> AdminUpdateSectionAsync(
        Guid id,
        UpdateHomeSectionBody body,
        PageCompositionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            locale,
            tenantId => composer.AdminUpdateSectionAsync(tenantId, locale, id, body, cancellationToken));

    private static async Task<IResult> AdminAddSectionAsync(
        AddHomeSectionBody body,
        PageCompositionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            locale,
            tenantId => composer.AdminAddSectionAsync(tenantId, locale, body, cancellationToken),
            successStatusCode: StatusCodes.Status201Created);

    private static async Task<IResult> AdminRemoveSectionAsync(
        Guid id,
        PageCompositionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            locale,
            tenantId => composer.AdminRemoveSectionAsync(tenantId, locale, id, cancellationToken));

    private static async Task<IResult> AdminRestoreDefaultAsync(
        PageCompositionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        await AdminMutationAsync(
            request,
            session,
            tenant,
            guard,
            environment,
            cancellationToken,
            locale,
            tenantId => composer.AdminRestoreDefaultHomeAsync(tenantId, locale, cancellationToken));

    private static async Task<IResult> AdminMutationAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        string? locale,
        Func<Guid, Task<AdminHomeCompositionSnapshot>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var tenantId = PageCompositionPanelComposer.RequireTenantId(tenant);
            var result = await action(tenantId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
            var unknownType = ex.Message.Contains("کاتالوگ", StringComparison.Ordinal);
            var forbiddenKey = ex.Message.Contains("ممنوع", StringComparison.Ordinal) || ex.Message.Contains("ناشناخته", StringComparison.Ordinal);
            var errorCode = missing
                ? "page-composition.section.missing"
                : unknownType
                    ? "page-composition.section-type.rejected"
                    : forbiddenKey
                        ? "page-composition.config.rejected"
                        : "page-composition.mutation.rejected";
            var statusCode = missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
            return Results.Json(new { title = missing ? "Not Found" : "Bad Request", errorCode, detail = ex.Message }, statusCode: statusCode);
        }
    }
}

/// <summary>بدنهٔ مرتب‌سازی sectionها.</summary>
public sealed record ReorderHomeSectionsBody(IReadOnlyList<Guid> SectionIds);

/// <summary>بدنهٔ افزودن section.</summary>
public sealed record AddHomeSectionBody(
    string SectionType,
    string? Variant,
    string? ConfigurationJson,
    bool IsVisible = true);

/// <summary>بدنهٔ به‌روزرسانی section.</summary>
public sealed record UpdateHomeSectionBody(
    bool? IsVisible,
    string? ConfigurationJson,
    string? Variant);
