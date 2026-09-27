using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Application.Customer;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Order.Endpoints.Customer;

/// <summary>
/// Module-owned customer Actor resolver for Order customer panel routes.
/// Uses the platform <see cref="ICurrentAuthenticatedUser"/> seam; no Host dependency.
/// </summary>
public sealed class OrderCustomerAuthorizer(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IOrderCustomerAuthorizer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Task<(Guid? ActorUserId, SemanticError? Error)> ResolveActorAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var actor = ResolveActor(httpContext, currentUser, environment);
        if (actor is null)
        {
            return Task.FromResult<(Guid?, SemanticError?)>(
                (null, new SemanticError(CustomerOrderErrors.SessionRequired)));
        }

        return Task.FromResult<(Guid?, SemanticError?)>((actor, null));
    }

    private static Guid? ResolveActor(
        HttpContext httpContext,
        ICurrentAuthenticatedUser currentUser,
        IHostEnvironment environment)
    {
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
