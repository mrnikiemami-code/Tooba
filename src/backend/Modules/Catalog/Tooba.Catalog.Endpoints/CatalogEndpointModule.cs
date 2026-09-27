using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints;

/// <summary>Thin composition for Catalog HTTP ownership (Admin AMC W1 foundation).</summary>
public static class CatalogEndpointModule
{
    /// <summary>
    /// Maps Catalog module HTTP routes. W1 leaves Host Admin Catalog routes in place;
    /// subsequent waves register audience groups here as files evacuate.
    /// </summary>
    public static IEndpointRouteBuilder MapCatalogModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }

    /// <summary>
    /// Registers Catalog endpoint presentation seams (admin authorizer).
    /// Error catalog / resources land with the first canonical Result endpoints.
    /// </summary>
    public static IServiceCollection AddCatalogEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ICatalogAdminAuthorizer, CatalogAdminAuthorizer>();
        return services;
    }
}
