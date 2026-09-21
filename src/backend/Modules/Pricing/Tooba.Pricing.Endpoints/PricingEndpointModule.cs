using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Pricing.Endpoints.Errors;
using Tooba.Pricing.Endpoints.Resources;

namespace Tooba.Pricing.Endpoints;

/// <summary>
/// Thin Pricing HTTP composition point. Seller price writes stay on the Offer seller route and call Pricing contracts.
/// </summary>
public static class PricingEndpointModule
{
    /// <summary>Registers the Pricing route group. No public Pricing route is mapped yet.</summary>
    public static IEndpointRouteBuilder MapPricingModule(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _ = app.MapGroup("/v1/pricing");
        return app;
    }

    /// <summary>Registers the Pricing error catalog and resource set.</summary>
    public static IServiceCollection AddPricingEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, PricingErrorResourceSet>();
        return services;
    }
}
