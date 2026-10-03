using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Party.Endpoints.Admin.Sellers;
using Tooba.Party.Endpoints.Seller;

namespace Tooba.Party.Endpoints;

/// <summary>ترکیب نازک مالکیت HTTP ماژول Party — بدون منطق کسب‌وکار.</summary>
public static class PartyEndpointModule
{
    /// <summary>
    /// Host composition seam retained for presentation registration.
    /// Error catalog/resources are owned by <c>PartyModule</c> (Infrastructure).
    /// </summary>
    public static IServiceCollection AddPartyEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
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
