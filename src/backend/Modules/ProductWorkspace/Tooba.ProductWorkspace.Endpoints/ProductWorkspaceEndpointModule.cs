using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.ProductWorkspace.Endpoints.Admin;

namespace Tooba.ProductWorkspace.Endpoints;

/// <summary>
/// ProductWorkspace HTTP ownership composition. W18 maps zero routes; Host retains
/// ownership of the remaining Admin aggregate surface under <c>/v1/admin/products</c>.
/// </summary>
public static class ProductWorkspaceEndpointModule
{
    /// <summary>
    /// Maps module-owned ProductWorkspace endpoints. Intentionally empty in W18 —
    /// must not conflict with the existing Host Admin ProductWorkspace route map name.
    /// </summary>
    public static IEndpointRouteBuilder MapProductWorkspaceModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        // W18: zero route maps. W19+ migrates aggregate reads here without inventing architecture mid-move.
        return app;
    }

    /// <summary>
    /// Registers ProductWorkspace endpoint presentation seams. Not invoked from Host in W18
    /// (no route consumers yet); available for W19 wiring.
    /// </summary>
    public static IServiceCollection AddProductWorkspaceEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IProductWorkspaceAdminAuthorizer, ProductWorkspaceAdminAuthorizer>();
        return services;
    }
}
