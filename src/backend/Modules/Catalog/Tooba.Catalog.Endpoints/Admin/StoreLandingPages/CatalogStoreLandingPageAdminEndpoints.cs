using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.StoreLandingPages.Commands;
using Tooba.Catalog.Application.StoreLandingPages.Models;
using Tooba.Catalog.Application.StoreLandingPages.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.StoreLandingPages;

/// <summary>Admin Store Landing Page HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogStoreLandingPageAdminEndpoints
{
    /// <summary>Maps /v1/admin/pages* routes.</summary>
    public static void MapCatalogStoreLandingPageAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/pages");
        admin.MapGet("/", ListAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapGet("/{pageId:guid}", GetAsync);
        admin.MapGet("/{pageId:guid}/preview", PreviewAsync);
        admin.MapPut("/{pageId:guid}", UpdateAsync);
        admin.MapPut("/{pageId:guid}/status", SetStatusAsync);
        admin.MapDelete("/{pageId:guid}", DeletePageAsync);
        admin.MapPut("/home", SetHomeAsync);
        admin.MapGet("/home", GetHomeAdminAsync);
        admin.MapGet("/{pageId:guid}/sections", ListSectionsAsync);
        admin.MapPost("/{pageId:guid}/sections", AddSectionAsync);
        admin.MapPut("/{pageId:guid}/sections/composition", ReplaceCompositionAsync);
        admin.MapPut("/{pageId:guid}/sections/reorder", ReorderSectionsAsync);
        admin.MapPut("/{pageId:guid}/sections/{sectionId:guid}", UpdateSectionAsync);
        admin.MapPut("/{pageId:guid}/sections/{sectionId:guid}/enabled", SetSectionEnabledAsync);
        admin.MapDelete("/{pageId:guid}/sections/{sectionId:guid}", DeleteSectionAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListStoreLandingPagesQuery(), cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid pageId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetStoreLandingPageQuery(pageId), cancellationToken));
    }

    private static async Task<IResult> PreviewAsync(
        Guid pageId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new PreviewStoreLandingPageQuery(pageId), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        StoreLandingPageWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new CreateStoreLandingPageAdminCommand(body), cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid pageId,
        StoreLandingPageWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new UpdateStoreLandingPageAdminCommand(pageId, body), cancellationToken));
    }

    private static async Task<IResult> DeletePageAsync(
        Guid pageId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new DeleteStoreLandingPageAdminCommand(pageId), cancellationToken));
    }

    private static async Task<IResult> SetStatusAsync(
        Guid pageId,
        StoreLandingPageWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SetStoreLandingPageStatusAdminCommand(pageId, body.Status),
            cancellationToken));
    }

    private static async Task<IResult> SetHomeAsync(
        StoreHomeSelectionWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new SetStoreHomePageAdminCommand(body.HomePageId), cancellationToken));
    }

    private static async Task<IResult> GetHomeAdminAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetStoreHomeSelectionQuery(), cancellationToken));
    }

    private static async Task<IResult> ListSectionsAsync(
        Guid pageId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListStoreLandingPageSectionsQuery(pageId), cancellationToken));
    }

    private static async Task<IResult> AddSectionAsync(
        Guid pageId,
        StoreLandingPageSectionWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new AddStoreLandingPageSectionAdminCommand(pageId, body),
            cancellationToken));
    }

    private static async Task<IResult> ReplaceCompositionAsync(
        Guid pageId,
        StoreLandingPageCompositionReplaceRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new ReplaceStoreLandingPageCompositionAdminCommand(pageId, body.Sections),
            cancellationToken));
    }

    private static async Task<IResult> UpdateSectionAsync(
        Guid pageId,
        Guid sectionId,
        StoreLandingPageSectionWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpdateStoreLandingPageSectionAdminCommand(pageId, sectionId, body),
            cancellationToken));
    }

    private static async Task<IResult> SetSectionEnabledAsync(
        Guid pageId,
        Guid sectionId,
        StoreLandingPageSectionEnabledRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SetStoreLandingPageSectionEnabledAdminCommand(pageId, sectionId, body.IsEnabled),
            cancellationToken));
    }

    private static async Task<IResult> ReorderSectionsAsync(
        Guid pageId,
        StoreLandingPageSectionReorderRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new ReorderStoreLandingPageSectionsAdminCommand(pageId, body.SectionIds),
            cancellationToken));
    }

    private static async Task<IResult> DeleteSectionAsync(
        Guid pageId,
        Guid sectionId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new DeleteStoreLandingPageSectionAdminCommand(pageId, sectionId),
            cancellationToken));
    }
}
