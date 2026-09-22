using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Support.Endpoints.Admin;
using Tooba.Support.Endpoints.Customer;
using Tooba.Support.Endpoints.Errors;
using Tooba.Support.Endpoints.Seller;

namespace Tooba.Support.Endpoints;

/// <summary>Thin composition for Support HTTP ownership.</summary>
public static class SupportEndpointModule
{
    /// <summary>Maps Support customer/seller/admin routes.</summary>
    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var customer = app.MapGroup("/v1/customer/support");
        SupportCustomerEndpoints.Map(customer);
        var seller = app.MapGroup("/v1/seller/support");
        SupportSellerEndpoints.Map(seller);
        var admin = app.MapGroup("/v1/admin/support");
        SupportAdminEndpoints.Map(admin);
        return app;
    }

    /// <summary>Registers Support error catalog for ApiResponseFactory.</summary>
    public static IServiceCollection AddSupportEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, SupportErrorCatalogContributor>();
        return services;
    }
}
