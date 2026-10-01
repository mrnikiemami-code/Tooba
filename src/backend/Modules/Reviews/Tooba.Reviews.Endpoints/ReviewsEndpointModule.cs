using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Reviews.Endpoints.Admin;
using Tooba.Reviews.Endpoints.Customer;
using Tooba.Reviews.Endpoints.Errors;
using Tooba.Reviews.Endpoints.Resources;
using Tooba.Reviews.Endpoints.Seller;
using Tooba.Reviews.Endpoints.Storefront;

namespace Tooba.Reviews.Endpoints;

/// <summary>ترکیب مالکیت HTTP ماژول Reviews — Host/Reviews HOST_ZERO.</summary>
public static class ReviewsEndpointModule
{
    /// <summary>مسیرهای عمومی، مشتری، فروشنده و Admin Reviews را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapReviewsModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        ReviewsStorefrontEndpoints.Map(app);
        ReviewsCustomerEndpoints.Map(app);
        ReviewsSellerEndpoints.Map(app);
        ReviewsAdminEndpoints.Map(app);
        return app;
    }

    /// <summary>ثبت presentation seams (authorizers + actor resolver + error catalog/resources).</summary>
    public static IServiceCollection AddReviewsEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IReviewsAdminAuthorizer, ReviewsAdminAuthorizer>();
        services.AddScoped<IReviewsCustomerActorResolver, ReviewsCustomerActorResolver>();
        services.AddSingleton<IErrorCatalogContributor, ReviewsErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, ReviewsErrorResourceSet>();
        return services;
    }
}
