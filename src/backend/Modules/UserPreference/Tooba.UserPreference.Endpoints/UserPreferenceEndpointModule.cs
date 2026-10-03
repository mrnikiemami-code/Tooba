using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.UserPreference.Endpoints.Admin;
using Tooba.UserPreference.Endpoints.Customer;

namespace Tooba.UserPreference.Endpoints;

/// <summary>UserPreference HTTP ownership — Host Preferences HOST_ZERO.</summary>
public static class UserPreferenceEndpointModule
{
    /// <summary>Maps customer locale, admin locale, and admin UI preference routes.</summary>
    public static IEndpointRouteBuilder MapUserPreferenceModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        UserPreferenceCustomerEndpoints.Map(app.MapGroup("/v1/customer/preferences"));
        UserPreferenceAdminEndpoints.Map(app.MapGroup("/v1/admin/operator/preferences"));
        UiPreferenceAdminEndpoints.Map(app.MapGroup("/v1/admin/ui-preferences"));
        return app;
    }

    /// <summary>
    /// Registers actor resolver. Error catalog/resource set are registered by <c>UserPreferenceModule</c>.
    /// </summary>
    public static IServiceCollection AddUserPreferenceEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IUserPreferenceCustomerActorResolver, UserPreferenceCustomerActorResolver>();
        return services;
    }
}
