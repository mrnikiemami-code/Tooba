using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Wallet.Endpoints.Admin;
using Tooba.Wallet.Endpoints.Customer;
using Tooba.Wallet.Endpoints.Errors;

namespace Tooba.Wallet.Endpoints;

/// <summary>Thin composition for Wallet HTTP ownership.</summary>
public static class WalletEndpointModule
{
    /// <summary>Maps Wallet customer/admin routes.</summary>
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        WalletCustomerEndpoints.Map(app);
        WalletAdminEndpoints.Map(app);
        return app;
    }

    /// <summary>Registers Wallet error catalog for ApiResponseFactory.</summary>
    public static IServiceCollection AddWalletEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, WalletErrorCatalogContributor>();
        return services;
    }
}
