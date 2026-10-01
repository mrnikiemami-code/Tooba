using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Localization.Endpoints.Admin;
using Tooba.Localization.Endpoints.Errors;

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

    /// <summary>Registers Localization endpoint presentation (error catalog).</summary>
    public static IServiceCollection AddLocalizationEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, LocalizationErrorCatalogContributor>();
        return services;
    }
}
