using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductPublishing.Commands;
using Tooba.Catalog.Contracts.Errors;
using Tooba.OperatorProfile.Contracts;
using Tooba.ProductWorkspace.Application.Composition.Models;
using Tooba.ProductWorkspace.Application.Composition.Queries;
using Tooba.ProductWorkspace.Endpoints.Admin;

namespace Tooba.ProductWorkspace.Endpoints;

/// <summary>
/// ProductWorkspace HTTP ownership under <c>/v1/admin/products</c>.
/// W19: aggregate GET. W26: lifecycle publish / unpublish / archive / restore.
/// </summary>
public static class ProductWorkspaceEndpointModule
{
    /// <summary>Maps module-owned ProductWorkspace endpoints.</summary>
    public static IEndpointRouteBuilder MapProductWorkspaceModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/admin/products");
        group.MapGet("/{productId:guid}", GetProductWorkspaceAsync);
        group.MapPost("/{productId:guid}/publish", PublishAsync);
        group.MapPost("/{productId:guid}/unpublish", UnpublishAsync);
        group.MapPost("/{productId:guid}/archive", ArchiveAsync);
        group.MapPost("/{productId:guid}/restore", RestoreAsync);
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

    private static async Task<IResult> PublishAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateLifecycleAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new PublishProductCommand(productId));

    private static async Task<IResult> UnpublishAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateLifecycleAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new UnpublishProductCommand(productId));

    private static async Task<IResult> ArchiveAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateLifecycleAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new ArchiveProductCommand(productId));

    private static async Task<IResult> RestoreAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        await MutateLifecycleAsync(
            productId,
            sender,
            authorizer,
            api,
            httpContext,
            cancellationToken,
            new RestoreProductCommand(productId));

    private static async Task<IResult> MutateLifecycleAsync(
        Guid productId,
        ISender sender,
        IProductWorkspaceAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        IRequest<Result> command)
    {
        var actorUserId = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        var permissions = ReadPermissions(httpContext.Request);
        if (!permissions.CanPublish)
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
        return api.From(workspace);
    }
}
