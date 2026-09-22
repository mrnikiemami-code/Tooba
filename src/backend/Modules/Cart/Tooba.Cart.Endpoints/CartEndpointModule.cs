using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Cart.Endpoints.Errors;
using Tooba.Cart.Endpoints.Resources;
using Tooba.Cart.Endpoints.Storefront;

namespace Tooba.Cart.Endpoints;

/// <summary>Thin composition for Cart HTTP ownership.</summary>
public static class CartEndpointModule
{
    /// <summary>Maps Cart storefront routes under <c>/v1/storefront</c>.</summary>
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/storefront");
        CartStorefrontEndpoints.Map(group);
        return app;
    }

    /// <summary>Registers Cart error catalog and localization resources.</summary>
    public static IServiceCollection AddCartEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, CartErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, CartErrorResourceSet>();
        return services;
    }
}
