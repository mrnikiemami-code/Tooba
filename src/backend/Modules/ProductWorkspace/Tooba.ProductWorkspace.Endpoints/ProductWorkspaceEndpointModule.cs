using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductIdentity.Commands;
using Tooba.Catalog.Application.ProductIdentity.Models;
using Tooba.Catalog.Application.ProductPublishing.Commands;
using Tooba.Catalog.Application.ProductTaxonomy.Commands;
using Tooba.Catalog.Application.ProductTaxonomy.Models;
using Tooba.Catalog.Application.Variants.Commands;
using Tooba.Catalog.Application.Variants.Models;
using Tooba.Catalog.Contracts.Errors;
using Tooba.OperatorProfile.Contracts;
using Tooba.ProductWorkspace.Application.Composition.Models;
using Tooba.ProductWorkspace.Application.Composition.Queries;
using Tooba.ProductWorkspace.Endpoints.Admin;

namespace Tooba.ProductWorkspace.Endpoints;

/// <summary>
/// ProductWorkspace HTTP ownership under <c>/v1/admin/products</c>.
/// W19: aggregate GET. W26: lifecycle. W27: variant create/patch.
/// W29: create / catalog-title / core / quantity-policy.
/// W30: category / additional categories / brand.
/// </summary>
public static class ProductWorkspaceEndpointModule
{
    /// <summary>Maps module-owned ProductWorkspace endpoints.</summary>
    public static IEndpointRouteBuilder MapProductWorkspaceModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/admin/products");
        group.MapGet("/{productId:guid}", GetProductWorkspaceAsync);
        group.MapPost("/", CreateProductAsync);
        group.MapPatch("/{productId:guid}/catalog-title", PatchCatalogTitleAsync);
        group.MapPatch("/{productId:guid}/core", PatchCoreAsync);
        group.MapPatch("/{productId:guid}/quantity-policy", PatchQuantityPolicyAsync);
        group.MapPut("/{productId:guid}/category", AssignCategoryAsync);
        group.MapPost("/{productId:guid}/categories/additional", AddAdditionalCategoryAsync);
        group.MapDelete("/{productId:guid}/categories/additional/{categoryId:guid}", RemoveAdditionalCategoryAsync);
        group.MapPut("/{productId:guid}/brand", AssignBrandAsync);
        group.MapPost("/{productId:guid}/publish", PublishAsync);
        group.MapPost("/{productId:guid}/unpublish", UnpublishAsync);
        group.MapPost("/{productId:guid}/archive", ArchiveAsync);
        group.MapPost("/{productId:guid}/restore", RestoreAsync);
        group.MapPost("/{productId:guid}/variants", CreateVariantAsync);
        group.MapPatch("/{productId:guid}/variants/{variantId:guid}", PatchVariantAsync);
        return app;
    }

    /// <summary>Registers ProductWorkspace endpoint presentation seams.</summary>
    public static IServiceCollection AddProductWorkspaceEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IProductWorkspaceAdminAuthorizer, ProductWorkspaceAdminAuthorizer>();
        return services;
    }

    private static ProductWorkspacePermissions ReadPermissions(HttpRequest request)
    {
        var scope = request.Headers["X-Tooba-Workspace-Scope"].ToString();
        if (string.Equals(scope, "view", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductWorkspacePermissions(true, false, false, false, false);
        }

        return new ProductWorkspacePermissions(true, true, true, true, true);
    }

    private static async Task BindCatalogActorAsync(
        HttpContext http,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = http.RequestServices.GetRequiredService<ICatalogActorContext>();
        actor.ActorUserId = actorUserId;

        var displays = http.RequestServices.GetService<IActorDisplayLookup>();
        if (displays is null)
        {
            actor.ActorDisplayName = "اپراتور";
            return;
        }

        var map = await displays.GetActorDisplaysAsync([actorUserId], cancellationToken);
        if (map.TryGetValue(actorUserId, out var projection)
            && !string.IsNullOrWhiteSpace(projection.DisplayName))
        {
            actor.ActorDisplayName = projection.DisplayName.Trim();
            return;
        }

        actor.ActorDisplayName = "اپراتور";
    }

    private static async Task<IResult> GetProductWorkspaceAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        var result = await sender.Send(
            new GetProductWorkspaceQuery(productId, ReadPermissions(httpContext.Request)),
            cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> CreateProductAsync(
        WorkspaceProductCreateWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        var permissions = ReadPermissions(httpContext.Request);
        if (!permissions.CanEditCatalog)
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await BindCatalogActorAsync(httpContext, actorUserId, cancellationToken);
        var mutation = await sender.Send(new CreateWorkspaceProductCommand(body), cancellationToken);
        if (mutation.IsFailure)
        {
            return api.From(mutation);
        }

        var workspace = await sender.Send(
            new GetProductWorkspaceQuery(mutation.Value, permissions),
            cancellationToken);
        if (workspace.IsFailure)
        {
            return api.From(workspace);
        }

        return Results.Json(workspace.Value, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> PatchCatalogTitleAsync(
        Guid productId,
        WorkspaceProductCatalogTitleWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new UpdateProductCatalogTitleCommand(productId, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> PatchCoreAsync(
        Guid productId,
        WorkspaceProductCoreUpdateWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new UpdateProductCoreCommand(productId, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> PatchQuantityPolicyAsync(
        Guid productId,
        WorkspaceProductQuantityPolicyWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new UpdateProductQuantityPolicyCommand(productId, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> AssignCategoryAsync(
        Guid productId,
        WorkspaceProductCategoryAssignWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new AssignProductCategoryCommand(productId, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> AddAdditionalCategoryAsync(
        Guid productId,
        WorkspaceProductAdditionalCategoryWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new AddAdditionalCategoryCommand(productId, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> RemoveAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        DateTimeOffset? expectedUpdatedAt,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (expectedUpdatedAt is null)
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.CategoryAssignmentStale));
        }

        return await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new RemoveAdditionalCategoryCommand(productId, categoryId, expectedUpdatedAt.Value),
            p => p.CanEditCatalog,
            created: false);
    }

    private static async Task<IResult> AssignBrandAsync(
        Guid productId,
        WorkspaceProductBrandAssignWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new AssignProductBrandCommand(productId, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> PublishAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new PublishProductCommand(productId),
            p => p.CanPublish,
            created: false);

    private static async Task<IResult> UnpublishAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new UnpublishProductCommand(productId),
            p => p.CanPublish,
            created: false);

    private static async Task<IResult> ArchiveAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new ArchiveProductCommand(productId),
            p => p.CanPublish,
            created: false);

    private static async Task<IResult> RestoreAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new RestoreProductCommand(productId),
            p => p.CanPublish,
            created: false);

    private static async Task<IResult> CreateVariantAsync(
        Guid productId,
        WorkspaceVariantCreateWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new CreateProductWorkspaceVariantCommand(productId, body),
            p => p.CanEditCatalog,
            created: true);

    private static async Task<IResult> PatchVariantAsync(
        Guid productId,
        Guid variantId,
        WorkspaceVariantPatchWriteModel body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new PatchProductWorkspaceVariantCommand(productId, variantId, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> MutateAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        IRequest<Result> command,
        Func<ProductWorkspacePermissions, bool> allow,
        bool created)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        var permissions = ReadPermissions(httpContext.Request);
        if (!allow(permissions))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await BindCatalogActorAsync(httpContext, actorUserId, cancellationToken);
        var mutation = await sender.Send(command, cancellationToken);
        if (mutation.IsFailure)
        {
            return api.From(mutation);
        }

        var workspace = await sender.Send(
            new GetProductWorkspaceQuery(productId, permissions),
            cancellationToken);
        if (workspace.IsFailure)
        {
            return api.From(workspace);
        }

        return created
            ? Results.Json(workspace.Value, statusCode: StatusCodes.Status201Created)
            : api.From(workspace);
    }
}
