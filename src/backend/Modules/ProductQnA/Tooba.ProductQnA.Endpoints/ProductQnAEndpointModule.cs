using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.ProductQnA.Endpoints.Customer;
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

    /// <summary>
    /// Host composition seam for presentation registration.
    /// Error catalog/resources are owned by <c>ProductQnAModule</c> (Infrastructure).
    /// </summary>
    public static IServiceCollection AddProductQnAEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IProductQnACustomerActorResolver, ProductQnACustomerActorResolver>();
        return services;
    }
}
