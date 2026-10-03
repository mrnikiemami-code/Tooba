using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Wishlist.Endpoints.Customer;

namespace Tooba.Wishlist.Endpoints;

/// <summary>Wishlist HTTP ownership composition — Host Wishlist folder HOST_ZERO.</summary>
public static class WishlistEndpointModule
{
    /// <summary>Maps module-owned wishlist routes under /v1/customer/wishlist.</summary>
    public static IEndpointRouteBuilder MapWishlistModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/customer/wishlist");
        WishlistCustomerEndpoints.Map(group);
        return app;
    }

    /// <summary>
    /// Registers Wishlist presentation seams (actor resolver).
    /// Error catalog/resource set are registered by <c>WishlistModule</c>.
    /// </summary>
    public static IServiceCollection AddWishlistEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IWishlistCustomerActorResolver, WishlistCustomerActorResolver>();
        return services;
    }
}
