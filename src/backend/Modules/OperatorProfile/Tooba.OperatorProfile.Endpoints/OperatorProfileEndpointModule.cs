using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.OperatorProfile.Endpoints.Admin;

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

    /// <summary>
    /// Host composition seam retained for presentation registration.
    /// Error catalog/resources are owned by <c>OperatorProfileModule</c> (Infrastructure).
    /// </summary>
    public static IServiceCollection AddOperatorProfileEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
