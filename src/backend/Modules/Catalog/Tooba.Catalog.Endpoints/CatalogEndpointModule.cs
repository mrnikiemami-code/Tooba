using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Catalog.Endpoints.Admin;
using Tooba.Catalog.Endpoints.Admin.Attributes.Definitions;
using Tooba.Catalog.Endpoints.Admin.Attributes.ProductValues;
using Tooba.Catalog.Endpoints.Admin.Attributes.Schema;
using Tooba.Catalog.Endpoints.Admin.Brands;
using Tooba.Catalog.Endpoints.Admin.CatalogDemo;
using Tooba.Catalog.Endpoints.Admin.StoreLandingPages;
using Tooba.Catalog.Endpoints.Admin.StoreMenus;
using Tooba.Catalog.Endpoints.Admin.Categories;
using Tooba.Catalog.Endpoints.Admin.CategoryChanges;
using Tooba.Catalog.Endpoints.Admin.Facets;
using Tooba.Catalog.Endpoints.Admin.MegaMenu;
using Tooba.Catalog.Endpoints.Admin.ProductMedia;
using Tooba.Catalog.Endpoints.Admin.ProductHistory;
using Tooba.Catalog.Endpoints.Admin.ProductPublishing;
using Tooba.Catalog.Endpoints.Admin.ProductSeo;
using Tooba.Catalog.Endpoints.Admin.Settings;
using Tooba.Catalog.Endpoints.Admin.Tags;
using Tooba.Catalog.Endpoints.Admin.Units;
using Tooba.Catalog.Endpoints.Admin.Variants;
using Tooba.Catalog.Endpoints.Errors;
using Tooba.Catalog.Endpoints.Resources;
using Tooba.Catalog.Endpoints.Storefront.Categories;
using Tooba.Catalog.Endpoints.Storefront.Facets;
using Tooba.Catalog.Endpoints.Storefront.MegaMenu;
using Tooba.Catalog.Endpoints.Storefront.StoreLandingPages;
using Tooba.Catalog.Endpoints.Storefront.StoreMenus;

namespace Tooba.Catalog.Endpoints;

/// <summary>Thin composition for Catalog HTTP ownership.</summary>
public static class CatalogEndpointModule
{
    /// <summary>Maps Catalog module HTTP routes (Admin settings + units + tags + MegaMenu + Facets + Categories slices and later waves).</summary>
    public static IEndpointRouteBuilder MapCatalogModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapQuantitySettingsEndpoints();
        app.MapCheckoutAbuseSettingsEndpoints();
        app.MapCheckoutIdentitySettingsEndpoints();
        app.MapUnitOfMeasureEndpoints();
        app.MapCatalogTagEndpoints();
        app.MapCatalogMegaMenuAdminEndpoints();
        app.MapCatalogMegaMenuStorefrontEndpoints();
        app.MapCatalogFacetAdminEndpoints();
        app.MapCatalogFacetStorefrontEndpoints();
        app.MapCatalogCategoryAdminEndpoints();
        app.MapCatalogCategoryStorefrontEndpoints();
        app.MapCatalogAttributeDefinitionAdminEndpoints();
        app.MapCatalogCategoryAttributeSchemaAdminEndpoints();
        app.MapCatalogProductAttributeAdminEndpoints();
        app.MapCatalogProductVariantAdminEndpoints();
        app.MapCatalogProductCategoryChangeAdminEndpoints();
        app.MapCatalogProductMediaAdminEndpoints();
        app.MapCatalogProductSeoAdminEndpoints();
        app.MapCatalogProductHistoryAdminEndpoints();
        app.MapCatalogProductPublishReadinessAdminEndpoints();
        app.MapCatalogBrandOptionsAdminEndpoints();
        app.MapCatalogStoreLandingPageAdminEndpoints();
        app.MapCatalogStoreLandingPageStorefrontEndpoints();
        app.MapCatalogStoreMenuAdminEndpoints();
        app.MapCatalogStoreMenuStorefrontEndpoints();
        app.MapCatalogDemoDevEndpoints();
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
