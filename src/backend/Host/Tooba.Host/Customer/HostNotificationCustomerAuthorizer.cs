#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Host.Storefront;
using Tooba.Notification.Endpoints.Customer;

namespace Tooba.Host.Customer;

/// <summary>Host transport adapter for Notification customer Endpoints auth.</summary>
public sealed class HostNotificationCustomerAuthorizer : INotificationCustomerAuthorizer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    public Guid? TryResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var request = httpContext.Request;

        if (session.IsAuthenticated)
            return session.UserId;

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            return null;

        if (request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }
}
