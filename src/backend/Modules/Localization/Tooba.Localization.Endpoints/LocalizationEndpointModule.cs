using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Localization.Endpoints.Admin;

namespace Tooba.Localization.Endpoints;

/// <summary>Localization HTTP ownership — Host/Localization HOST_ZERO.</summary>
public static class LocalizationEndpointModule
{
    /// <summary>Maps admin language registry routes.</summary>
    public static IEndpointRouteBuilder MapLocalizationModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        LocaleAdminEndpoints.Map(app.MapGroup("/v1/admin/languages"));
        return app;
    }

    /// <summary>
    /// Host composition seam retained for presentation registration.
    /// Error catalog/resources are owned by <c>LocalizationModule</c> (Infrastructure).
    /// </summary>
    public static IServiceCollection AddLocalizationEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
