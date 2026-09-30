using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Story.Endpoints.Admin;
using Tooba.Story.Endpoints.Errors;
using Tooba.Story.Endpoints.Resources;
using Tooba.Story.Endpoints.Seller;
using Tooba.Story.Endpoints.Storefront;

namespace Tooba.Story.Endpoints;

/// <summary>ترکیب مالکیت HTTP ماژول Story — Host/Story HOST_ZERO.</summary>
public static class StoryEndpointModule
{
    /// <summary>مسیرهای عمومی، فروشنده و Admin Story را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapStoryModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        StoryStorefrontEndpoints.Map(app);
        StorySellerEndpoints.Map(app);
        StoryAdminEndpoints.Map(app);
        return app;
    }

    /// <summary>ثبت presentation seams (admin authorizer + error catalog/resources).</summary>
    public static IServiceCollection AddStoryEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IStoryAdminAuthorizer, StoryAdminAuthorizer>();
        services.AddSingleton<IErrorCatalogContributor, StoryErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, StoryErrorResourceSet>();
        return services;
    }
}
