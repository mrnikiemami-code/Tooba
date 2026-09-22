using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Tooba.Returns.Endpoints.Admin;
using Tooba.Returns.Endpoints.Customer;
using Tooba.Returns.Endpoints.Seller;

namespace Tooba.Returns.Endpoints;

/// <summary>Thin composition for Returns HTTP ownership.</summary>
public static class ReturnEndpointModule
{
    /// <summary>Maps Returns customer/seller/admin routes.</summary>
    public static IEndpointRouteBuilder MapReturnEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var customer = app.MapGroup("/v1/customer");
        ReturnCustomerEndpoints.Map(customer);
        var seller = app.MapGroup("/v1/seller");
        ReturnSellerEndpoints.Map(seller);
        var admin = app.MapGroup("/v1/admin");
        ReturnAdminEndpoints.Map(admin);
        return app;
    }
}
