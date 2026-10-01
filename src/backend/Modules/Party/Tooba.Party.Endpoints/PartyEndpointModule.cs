using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Party.Endpoints.Admin.Sellers;
using Tooba.Party.Endpoints.Errors;
using Tooba.Party.Endpoints.Resources;
using Tooba.Party.Endpoints.Seller;

namespace Tooba.Party.Endpoints;

/// <summary>ترکیب نازک مالکیت HTTP ماژول Party — بدون منطق کسب‌وکار.</summary>
public static class PartyEndpointModule
{
    /// <summary>کاتالوگ خطای Party و درزهای نمایشی آن را ثبت می‌کند.</summary>
    public static IServiceCollection AddPartyEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, PartyErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, PartyErrorResourceSet>();
        return services;
    }

    /// <summary>مسیرهای Party را ثبت می‌کند (تنظیمات فروشنده + Admin sellers list).</summary>
    public static IEndpointRouteBuilder MapPartyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var seller = app.MapGroup("/v1/seller/settings");
        PartySellerSettingsEndpoints.Map(seller);
        PartyAdminSellersEndpoints.Map(app);
        return app;
    }
}
