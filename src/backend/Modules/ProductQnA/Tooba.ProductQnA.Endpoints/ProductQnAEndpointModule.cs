using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.ProductQnA.Endpoints.Customer;
using Tooba.ProductQnA.Endpoints.Errors;
using Tooba.ProductQnA.Endpoints.Resources;
using Tooba.ProductQnA.Endpoints.Storefront;

namespace Tooba.ProductQnA.Endpoints;

/// <summary>ترکیب مالکیت HTTP ماژول ProductQnA — Host/ProductQnA HOST_ZERO.</summary>
public static class ProductQnAEndpointModule
{
    /// <summary>مسیرهای عمومی و مشتری ProductQnA را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapProductQnAModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        ProductQnAStorefrontEndpoints.Map(app);
        ProductQnACustomerEndpoints.Map(app);
        return app;
    }

    /// <summary>ثبت actor resolver و error catalog/resources.</summary>
    public static IServiceCollection AddProductQnAEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IProductQnACustomerActorResolver, ProductQnACustomerActorResolver>();
        services.AddSingleton<IErrorCatalogContributor, ProductQnAErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, ProductQnAErrorResourceSet>();
        return services;
    }
}
