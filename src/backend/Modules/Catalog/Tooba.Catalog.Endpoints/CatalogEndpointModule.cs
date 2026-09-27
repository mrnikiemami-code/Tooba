using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Catalog.Endpoints.Admin;
using Tooba.Catalog.Endpoints.Admin.Facets;
using Tooba.Catalog.Endpoints.Admin.MegaMenu;
using Tooba.Catalog.Endpoints.Admin.Settings;
using Tooba.Catalog.Endpoints.Admin.Tags;
using Tooba.Catalog.Endpoints.Admin.Units;
using Tooba.Catalog.Endpoints.Errors;
using Tooba.Catalog.Endpoints.Resources;
using Tooba.Catalog.Endpoints.Storefront.Facets;
using Tooba.Catalog.Endpoints.Storefront.MegaMenu;

namespace Tooba.Catalog.Endpoints;

/// <summary>Thin composition for Catalog HTTP ownership.</summary>
public static class CatalogEndpointModule
{
    /// <summary>Maps Catalog module HTTP routes (Admin settings + units + tags + MegaMenu + Facets slices and later waves).</summary>
    public static IEndpointRouteBuilder MapCatalogModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapQuantitySettingsEndpoints();
        app.MapUnitOfMeasureEndpoints();
        app.MapCatalogTagEndpoints();
        app.MapCatalogMegaMenuAdminEndpoints();
        app.MapCatalogMegaMenuStorefrontEndpoints();
        app.MapCatalogFacetAdminEndpoints();
        app.MapCatalogFacetStorefrontEndpoints();
        return app;
    }

    /// <summary>Registers Catalog endpoint presentation seams.</summary>
    public static IServiceCollection AddCatalogEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ICatalogAdminAuthorizer, CatalogAdminAuthorizer>();
        services.AddSingleton<IErrorCatalogContributor, CatalogErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, CatalogErrorResourceSet>();
        return services;
    }
}
