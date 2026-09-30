#pragma warning disable CS1591
using Tooba.Payment.Endpoints.Storefront;

namespace Tooba.Host.Security.Payment;

/// <summary>Host transport adapter for Payment storefront actor resolution.</summary>
internal sealed class HostPaymentStorefrontAuthorizer(CurrentAuthenticatedSession session) : IPaymentStorefrontAuthorizer
{
    public Guid? TryResolveAuthenticatedUserId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (session.IsAuthenticated && session.UserId is { } userId && userId != Guid.Empty)
            return userId;
        return null;
    }
}
