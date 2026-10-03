using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.PageComposition.Application.Admin.Commands;
using Tooba.PageComposition.Application.Admin.Queries;
using Tooba.PageComposition.Application.Models;
using Tooba.PageComposition.Application.Storefront.Queries;
using Tooba.PageComposition.Endpoints.Models;

namespace Tooba.PageComposition.Endpoints.Admin;

/// <summary>مرز HTTP مدیریتی PageComposition.</summary>
public static class PageCompositionAdminEndpoints
{
    /// <summary>مسیرهای Admin را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin/page-composition/home");
        admin.MapGet("", AdminGetHomeAsync);
        admin.MapGet("/catalog", AdminGetCatalogAsync);
        admin.MapPut("/reorder", AdminReorderAsync);
        admin.MapPut("/sections/{id:guid}", AdminUpdateSectionAsync);
        admin.MapPost("/sections", AdminAddSectionAsync);
        admin.MapDelete("/sections/{id:guid}", AdminRemoveSectionAsync);
        admin.MapPost("/restore-default", AdminRestoreDefaultAsync);
    }

    private static async Task<IResult> AdminGetCatalogAsync(
        ISender sender,
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            return Results.Json(await sender.Send(new GetSectionCatalogQuery(), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return PageCompositionHttpErrors.From(ex, api);
        }
    }

    private static async Task<IResult> AdminGetHomeAsync(
        ISender sender,
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = PageCompositionHttpErrors.RequireTenantId(tenant);
            return Results.Json(await sender.Send(
                new AdminGetHomeCompositionQuery(tenantId, locale), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return PageCompositionHttpErrors.From(ex, api);
        }
    }

    private static Task<IResult> AdminReorderAsync(
        ReorderHomeSectionsBody body,
        ISender sender,
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        AdminMutationAsync(
            auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new AdminReorderHomeSectionsCommand(tenantId, locale, body.SectionIds), cancellationToken));

    private static Task<IResult> AdminUpdateSectionAsync(
        Guid id,
        UpdateHomeSectionBody body,
        ISender sender,
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        AdminMutationAsync(
            auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new AdminUpdateHomeSectionCommand(
                    tenantId,
                    locale,
                    id,
                    new UpdateHomeSectionCommand(body.IsVisible, body.ConfigurationJson, body.Variant)),
                cancellationToken));

    private static Task<IResult> AdminAddSectionAsync(
        AddHomeSectionBody body,
        ISender sender,
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        AdminMutationAsync(
            auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new AdminAddHomeSectionCommand(
                    tenantId,
                    locale,
                    new AddHomeSectionCommand(
                        body.SectionType,
                        body.Variant ?? "default",
                        body.ConfigurationJson,
                        body.IsVisible)),
                cancellationToken),
            StatusCodes.Status201Created);

    private static Task<IResult> AdminRemoveSectionAsync(
        Guid id,
        ISender sender,
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        AdminMutationAsync(
            auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new AdminRemoveHomeSectionCommand(tenantId, locale, id), cancellationToken));

    private static Task<IResult> AdminRestoreDefaultAsync(
        ISender sender,
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        AdminMutationAsync(
            auth, http, tenant, api, cancellationToken,
            tenantId => sender.Send(
                new AdminRestoreDefaultHomeCompositionCommand(tenantId, locale), cancellationToken));

    private static async Task<IResult> AdminMutationAsync(
        IPageCompositionAdminAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Func<Guid, Task<AdminHomeCompositionSnapshot>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = PageCompositionHttpErrors.RequireTenantId(tenant);
            var result = await action(tenantId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return PageCompositionHttpErrors.From(ex, api);
        }
    }
}
