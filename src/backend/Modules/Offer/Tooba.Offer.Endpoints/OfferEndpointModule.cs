using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Offer.Endpoints.Errors;
using Tooba.Offer.Endpoints.Resources;
using Tooba.Offer.Endpoints.Seller;

namespace Tooba.Offer.Endpoints;

/// <summary>
/// ترکیب باریک مسیرهای HTTP مالک Offer.
/// </summary>
public static class OfferEndpointModule
{
    /// <summary>
    /// مسیرهای Offer فروشنده را زیر <c>/v1/seller</c> ثبت می‌کند.
    /// </summary>
    public static IEndpointRouteBuilder MapOfferModule(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/seller");
        OfferSellerEndpoints.Map(group);
        return app;
    }

    /// <summary>کاتالوگ خطا و منابع محلی‌سازی Offer را ثبت می‌کند.</summary>
    public static IServiceCollection AddOfferEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, OfferErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, OfferErrorResourceSet>();
        return services;
    }
}
