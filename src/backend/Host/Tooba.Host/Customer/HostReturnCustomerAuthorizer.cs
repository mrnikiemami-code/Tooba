#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Returns.Endpoints.Customer;

namespace Tooba.Host.Customer;

/// <summary>Host transport adapter for Returns customer Endpoints auth.</summary>
public sealed class HostReturnCustomerAuthorizer : IReturnCustomerAuthorizer
{
    public Guid? TryResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var request = httpContext.Request;

        if (session.IsAuthenticated && session.UserId is { } authenticated)
            return authenticated;

        if ((environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            && request.Headers.TryGetValue("X-Tooba-Dev-Actor-User-Id", out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return null;
    }
}
