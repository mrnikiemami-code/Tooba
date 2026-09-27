using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Presentation;
using Tooba.ProductWorkspace.Application.Composition.Models;
using Tooba.ProductWorkspace.Application.Composition.Queries;
using Tooba.ProductWorkspace.Endpoints.Admin;

namespace Tooba.ProductWorkspace.Endpoints;

/// <summary>
/// ProductWorkspace HTTP ownership. W19 owns aggregate GET only under <c>/v1/admin/products</c>.
/// </summary>
public static class ProductWorkspaceEndpointModule
{
    /// <summary>Maps module-owned ProductWorkspace endpoints (aggregate GET only in W19).</summary>
    public static IEndpointRouteBuilder MapProductWorkspaceModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/admin/products");
        group.MapGet("/{productId:guid}", GetProductWorkspaceAsync);
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
}
