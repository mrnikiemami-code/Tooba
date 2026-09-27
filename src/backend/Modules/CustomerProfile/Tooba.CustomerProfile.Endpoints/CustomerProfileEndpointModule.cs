using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.CustomerProfile.Application.Ports;
using Tooba.CustomerProfile.Endpoints.Customer;
using Tooba.CustomerProfile.Endpoints.CustomerDashboard;
using Tooba.CustomerProfile.Endpoints.Resources;

namespace Tooba.CustomerProfile.Endpoints;

/// <summary>
/// CustomerProfile HTTP ownership: profile routes + thin customer-account dashboard/dev-context presentation.
/// Does not transfer Order/Wishlist/AddressBook/Identity business ownership.
/// </summary>
public static class CustomerProfileEndpointModule
{
    /// <summary>Maps module-owned /v1/customer profile, dashboard, and dev-context routes.</summary>
    public static IEndpointRouteBuilder MapCustomerProfileModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/customer");
        CustomerAccountDashboardEndpoints.MapDashboard(group);
        CustomerProfileEndpoints.MapProfile(group);
        return app;
    }

    /// <summary>
    /// Registers customer-account presentation seams (actor resolver + display texts).
    /// Does not re-register Foundation-owned <c>customer.session.required</c>.
    /// </summary>
    public static IServiceCollection AddCustomerProfileEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ICustomerAccountActorResolver, CustomerAccountActorResolver>();
        services.AddSingleton<ICustomerAccountDisplayTexts, CustomerAccountDisplayTexts>();
        return services;
    }
}
