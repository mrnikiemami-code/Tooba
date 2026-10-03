using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.PageComposition.Endpoints.Admin;
using Tooba.PageComposition.Endpoints.Storefront;

namespace Tooba.PageComposition.Endpoints;

/// <summary>ترکیب مالکیت HTTP ماژول PageComposition — Host/PageComposition HOST_ZERO.</summary>
public static class PageCompositionEndpointModule
{
    /// <summary>مسیرهای عمومی و Admin Page Composition را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapPageCompositionModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        PageCompositionStorefrontEndpoints.Map(app);
        PageCompositionAdminEndpoints.Map(app);
        return app;
    }

    /// <summary>ثبت presentation seams (admin authorizer). Error catalog owns in Infrastructure.</summary>
    public static IServiceCollection AddPageCompositionEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IPageCompositionAdminAuthorizer, PageCompositionAdminAuthorizer>();
        return services;
    }
}
