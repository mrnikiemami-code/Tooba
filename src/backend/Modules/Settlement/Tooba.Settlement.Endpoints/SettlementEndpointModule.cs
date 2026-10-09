using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.Settlement.Contracts.Errors;
using Tooba.Settlement.Endpoints.Admin;
using Tooba.Settlement.Endpoints.Seller;

namespace Tooba.Settlement.Endpoints;

/// <summary>Thin composition for Settlement HTTP ownership.</summary>
public static class SettlementEndpointModule
{
    /// <summary>Maps Settlement seller and admin routes.</summary>
    public static IEndpointRouteBuilder MapSettlementEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var seller = app.MapGroup("/v1/seller");
        SettlementSellerEndpoints.Map(seller);
        var admin = app.MapGroup("/v1/admin");
        SettlementAdminEndpoints.Map(admin);
        return app;
    }

    /// <summary>Registers module-owned Settlement error resource set (bilingual localization).</summary>
    public static IServiceCollection AddSettlementEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorResourceSet, SettlementErrorResourceSet>();
        return services;
    }
}
