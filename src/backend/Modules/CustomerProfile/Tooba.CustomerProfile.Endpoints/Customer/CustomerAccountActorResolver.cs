using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.CustomerProfile.Endpoints.Customer;

/// <summary>
/// Neutral actor-authority seam for customer-account HTTP. Same semantics as prior Host CustomerPanel:
/// authenticated session first; Dev/Testing header; otherwise StorefrontGuestActor.
/// </summary>
public interface ICustomerAccountActorResolver
{
    /// <summary>Resolves actor; null means no trusted identity (401).</summary>
    Guid? ResolveActor(HttpContext httpContext);
}

/// <summary>
/// Actor resolver using platform <see cref="ICurrentAuthenticatedUser"/> and
/// <see cref="StorefrontGuestActor.ActorId"/> — no Host types, no duplicated guest constant.
/// </summary>
public sealed class CustomerAccountActorResolver(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : ICustomerAccountActorResolver
{
    /// <summary>Exact Development/Testing actor header name preserved from Host.</summary>
    public const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Guid? ResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (currentUser.IsAuthenticated && currentUser.UserId is { } authenticated)
        {
            return authenticated;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return null;
        }

        if (httpContext.Request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return StorefrontGuestActor.ActorId;
    }
}
