using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.OperatorProfile.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;
using Tooba.ProductWorkspace.Application.Composition.ProductManagement.Models;
using Tooba.ProductWorkspace.Application.Composition.ProductManagement.Queries;
using Tooba.ProductWorkspace.Contracts.Errors;
using Tooba.ProductWorkspace.Endpoints.Admin;

namespace Tooba.ProductWorkspace.Endpoints;

/// <summary>
/// ProductWorkspace HTTP ownership under <c>/v1/admin/products</c>.
/// W19: aggregate GET. W26: lifecycle. W27: variant create/patch.
/// W29: create / catalog-title / core / quantity-policy.
/// W30: category / additional categories / brand.
/// W31: list + grid query.
/// <para>
/// Every mutation is dispatched to a module-local CQRS command that reaches the Catalog write capability
/// through the Contracts-only <see cref="ICatalogAdminProductWorkspaceMutationGateway"/>. The endpoints
/// never reference Catalog Application commands, Catalog Application write models or the Catalog actor
/// context, so the module stays microservice-extractable.
/// </para>
/// </summary>
public static class ProductWorkspaceEndpointModule
{
    private const string DefaultActorDisplayName = "اپراتور";

    /// <summary>Maps module-owned ProductWorkspace endpoints.</summary>
    public static IEndpointRouteBuilder MapProductWorkspaceModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/admin/products");
        group.MapGet("/", ListProductsAsync);
        group.MapPost("/query", QueryProductGridAsync);
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

    private static async Task<IResult> ListProductsAsync(
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(new ListProductWorkspaceQuery(), cancellationToken));
    }

    private static async Task<IResult> QueryProductGridAsync(
        GridQueryRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(new QueryProductWorkspaceGridQuery(body), cancellationToken));
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

    private static async Task<CatalogAdminProductWorkspaceActor> ResolveActorAsync(
        HttpContext http,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var displays = http.RequestServices.GetService<IActorDisplayLookup>();
        if (displays is null)
        {
            return new CatalogAdminProductWorkspaceActor(actorUserId, DefaultActorDisplayName);
        }

        var map = await displays.GetActorDisplaysAsync([actorUserId], cancellationToken);
        if (map.TryGetValue(actorUserId, out var projection)
            && !string.IsNullOrWhiteSpace(projection.DisplayName))
        {
            return new CatalogAdminProductWorkspaceActor(actorUserId, projection.DisplayName.Trim());
        }

        return new CatalogAdminProductWorkspaceActor(actorUserId, DefaultActorDisplayName);
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
        CatalogAdminProductWorkspaceCreateRequest body,
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
            return api.FromFailure(new SemanticError(ProductWorkspaceErrorCodes.WorkspacePermissionDenied));
        }

        var actor = await ResolveActorAsync(httpContext, actorUserId, cancellationToken);
        var mutation = await sender.Send(
            new CreateWorkspaceProductCommand(actor, body),
            cancellationToken);
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

        return api.Created($"/v1/admin/products/{workspace.Value.ProductId}", workspace);
    }

    private static Task<IResult> PatchCatalogTitleAsync(
        Guid productId,
        CatalogAdminProductWorkspaceCatalogTitleRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new UpdateWorkspaceProductCatalogTitleCommand(productId, actor, body),
            p => p.CanEditCatalog,
            created: false);

    private static Task<IResult> PatchCoreAsync(
        Guid productId,
        CatalogAdminProductWorkspaceCoreRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new UpdateWorkspaceProductCoreCommand(productId, actor, body),
            p => p.CanEditCatalog,
            created: false);

    private static Task<IResult> PatchQuantityPolicyAsync(
        Guid productId,
        CatalogAdminProductWorkspaceQuantityPolicyRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new UpdateWorkspaceProductQuantityPolicyCommand(productId, actor, body),
            p => p.CanEditCatalog,
            created: false);

    private static Task<IResult> AssignCategoryAsync(
        Guid productId,
        CatalogAdminProductWorkspaceCategoryRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new AssignWorkspaceProductCategoryCommand(productId, actor, body),
            p => p.CanEditCatalog,
            created: false);

    private static Task<IResult> AddAdditionalCategoryAsync(
        Guid productId,
        CatalogAdminProductWorkspaceAdditionalCategoryRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new AddWorkspaceProductAdditionalCategoryCommand(productId, actor, body),
            p => p.CanEditCatalog,
            created: false);

    private static Task<IResult> RemoveAdditionalCategoryAsync(
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
            return Task.FromResult(
                api.FromFailure(new SemanticError(ProductWorkspaceErrorCodes.CategoryAssignmentStale)));
        }

        return MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new RemoveWorkspaceProductAdditionalCategoryCommand(
                productId,
                categoryId,
                actor,
                expectedUpdatedAt.Value),
            p => p.CanEditCatalog,
            created: false);
    }

    private static Task<IResult> AssignBrandAsync(
        Guid productId,
        CatalogAdminProductWorkspaceBrandRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new AssignWorkspaceProductBrandCommand(productId, actor, body),
            p => p.CanEditCatalog,
            created: false);

    private static Task<IResult> PublishAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new PublishWorkspaceProductCommand(productId, actor),
            p => p.CanPublish,
            created: false);

    private static Task<IResult> UnpublishAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new UnpublishWorkspaceProductCommand(productId, actor),
            p => p.CanPublish,
            created: false);

    private static Task<IResult> ArchiveAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new ArchiveWorkspaceProductCommand(productId, actor),
            p => p.CanPublish,
            created: false);

    private static Task<IResult> RestoreAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new RestoreWorkspaceProductCommand(productId, actor),
            p => p.CanPublish,
            created: false);

    private static Task<IResult> CreateVariantAsync(
        Guid productId,
        CatalogAdminProductWorkspaceVariantCreateRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new CreateWorkspaceProductVariantCommand(productId, actor, body),
            p => p.CanEditCatalog,
            created: true);

    private static Task<IResult> PatchVariantAsync(
        Guid productId,
        Guid variantId,
        CatalogAdminProductWorkspaceVariantPatchRequest body,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        MutateAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            actor => new PatchWorkspaceProductVariantCommand(productId, variantId, actor, body),
            p => p.CanEditCatalog,
            created: false);

    private static async Task<IResult> MutateAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        Func<CatalogAdminProductWorkspaceActor, IRequest<Result>> commandFactory,
        Func<ProductWorkspacePermissions, bool> allow,
        bool created)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        var permissions = ReadPermissions(httpContext.Request);
        if (!allow(permissions))
        {
            return api.FromFailure(new SemanticError(ProductWorkspaceErrorCodes.WorkspacePermissionDenied));
        }

        var actor = await ResolveActorAsync(httpContext, actorUserId, cancellationToken);
        var mutation = await sender.Send(commandFactory(actor), cancellationToken);
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
            ? api.Created($"/v1/admin/products/{workspace.Value.ProductId}", workspace)
            : api.From(workspace);
    }
}
