using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Identity.Endpoints.Auth;

namespace Tooba.Identity.Endpoints;

/// <summary>Thin Identity module HTTP composition root.</summary>
public static class IdentityEndpointModule
{
    public static IEndpointRouteBuilder MapIdentityModuleEndpoints(this IEndpointRouteBuilder app, bool enableCors = false)
    {
        ArgumentNullException.ThrowIfNull(app);
        IdentityAuthEndpoints.Map(app, enableCors);
        return app;
    }

    public static IServiceCollection AddIdentityEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
