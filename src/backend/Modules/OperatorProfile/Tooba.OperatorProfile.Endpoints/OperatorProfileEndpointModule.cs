using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.OperatorProfile.Endpoints.Admin;
using Tooba.OperatorProfile.Endpoints.Errors;

namespace Tooba.OperatorProfile.Endpoints;

/// <summary>OperatorProfile HTTP ownership — Host OperatorProfile HOST_ZERO.</summary>
public static class OperatorProfileEndpointModule
{
    /// <summary>Maps admin operator profile routes.</summary>
    public static IEndpointRouteBuilder MapOperatorProfileModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        OperatorProfileAdminEndpoints.Map(app.MapGroup("/v1/admin/operator/profile"));
        return app;
    }

    /// <summary>Registers OperatorProfile endpoint presentation (error catalog). Authorizer is Host-owned.</summary>
    public static IServiceCollection AddOperatorProfileEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, OperatorProfileErrorCatalogContributor>();
        return services;
    }
}
