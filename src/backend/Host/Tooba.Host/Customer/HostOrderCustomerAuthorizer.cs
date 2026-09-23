using Tooba.BuildingBlocks;
using Tooba.Host.Storefront;
using Tooba.Order.Application.Customer;
using Tooba.Order.Endpoints;

namespace Tooba.Host.Customer;

/// <summary>Host transport adapter — customer Actor for Order customer panel routes.</summary>
public sealed class HostOrderCustomerAuthorizer : IOrderCustomerAuthorizer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Task<(Guid? ActorUserId, SemanticError? Error)> ResolveActorAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var actor = ResolveActor(httpContext.Request, session, environment);
        if (actor is null)
        {
            return Task.FromResult<(Guid?, SemanticError?)>(
                (null, new SemanticError(CustomerOrderErrors.SessionRequired)));
        }

        return Task.FromResult<(Guid?, SemanticError?)>((actor, null));
    }

    private static Guid? ResolveActor(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment)
    {
        if (session.IsAuthenticated)
        {
            return session.UserId;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return null;
        }

        if (request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId;
    }
}
