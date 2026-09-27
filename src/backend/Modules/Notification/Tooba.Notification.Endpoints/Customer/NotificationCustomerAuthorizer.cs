using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Notification.Endpoints.Customer;

/// <summary>
/// Module-owned customer Actor resolver for Notification customer routes.
/// Uses the platform <see cref="ICurrentAuthenticatedUser"/> seam; no Host dependency.
/// </summary>
public sealed class NotificationCustomerAuthorizer(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : INotificationCustomerAuthorizer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Guid? TryResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (currentUser.IsAuthenticated && currentUser.UserId is { } authenticated)
            return authenticated;

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            return null;

        if (httpContext.Request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return StorefrontGuestActor.ActorId;
    }
}
