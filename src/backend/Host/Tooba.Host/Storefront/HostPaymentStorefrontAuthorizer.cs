#pragma warning disable CS1591
using Tooba.Payment.Endpoints.Storefront;

namespace Tooba.Host.Storefront;

/// <summary>Host transport adapter for Payment storefront actor resolution.</summary>
public sealed class HostPaymentStorefrontAuthorizer : IPaymentStorefrontAuthorizer
{
    public Guid? TryResolveAuthenticatedUserId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        if (session.IsAuthenticated && session.UserId is { } userId && userId != Guid.Empty)
            return userId;
        return null;
    }
}
