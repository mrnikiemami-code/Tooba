using Microsoft.AspNetCore.Http;

namespace Tooba.Notification.Endpoints.Customer;

/// <summary>Neutral customer auth seam for Notification Endpoints (Host implements).</summary>
public interface INotificationCustomerAuthorizer
{
    /// <summary>Resolves customer actor user id, or null when production-unauthenticated.</summary>
    Guid? TryResolveActor(HttpContext httpContext);
}
