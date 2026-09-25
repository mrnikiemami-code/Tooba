using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Fulfillment.Endpoints.Admin;
using Tooba.Fulfillment.Endpoints.Customer;
using Tooba.Fulfillment.Endpoints.Seller;
using Tooba.Fulfillment.Endpoints.Shipping;

namespace Tooba.Fulfillment.Endpoints;

/// <summary>Thin composition for Fulfillment HTTP ownership.</summary>
public static class FulfillmentEndpointModule
{
    /// <summary>Maps Fulfillment seller/admin/customer/shipping routes.</summary>
    public static IEndpointRouteBuilder MapFulfillmentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var seller = app.MapGroup("/v1/seller");
        FulfillmentSellerEndpoints.Map(seller);
        var admin = app.MapGroup("/v1/admin");
        FulfillmentAdminEndpoints.Map(admin);
        ShippingServiceEndpoints.Map(admin);
        ShippingMethodsEndpoints.Map(app);
        var customer = app.MapGroup("/v1/customer");
        FulfillmentCustomerEndpoints.Map(customer);
        return app;
    }

    /// <summary>Registers module-owned Fulfillment authorizer implementations.</summary>
    public static IServiceCollection AddFulfillmentEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IFulfillmentAdminAuthorizer, FulfillmentAdminAuthorizer>();
        services.AddScoped<IFulfillmentCustomerAuthorizer, FulfillmentCustomerAuthorizer>();
        services.AddScoped<IFulfillmentSellerAuthorizer, FulfillmentSellerAuthorizer>();
        return services;
    }
}
