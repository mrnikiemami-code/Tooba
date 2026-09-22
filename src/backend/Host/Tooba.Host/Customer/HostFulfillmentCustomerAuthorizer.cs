#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Endpoints.Customer;
using Tooba.Host.Storefront;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Host.Customer;

/// <summary>Host transport adapter — customer/guest checkout ownership for Fulfillment list.</summary>
public sealed class HostFulfillmentCustomerAuthorizer : IFulfillmentCustomerAuthorizer
{
    public async Task<SemanticError?> EnsureCanViewCheckoutAsync(
        HttpContext httpContext, Guid checkoutId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var ownership = httpContext.RequestServices.GetRequiredService<ICustomerCheckoutOwnershipReader>();
        var carts = httpContext.RequestServices.GetRequiredService<ICartQueryGateway>();

        var actor = ResolveCustomerActor(httpContext.Request, session, environment);
        var guestSecret = ReadGuestSecret(httpContext.Request);
        if (actor is null && string.IsNullOrWhiteSpace(guestSecret))
            return new SemanticError(FulfillmentErrorCodes.CustomerActorMissing);

        var checkout = await ownership.GetAsync(checkoutId, cancellationToken);
        if (checkout is null)
            return new SemanticError(FulfillmentErrorCodes.CustomerOrderMissing);

        var ownedByActor = actor is not null && checkout.PlacedByUserId == actor.Value;
        if (checkout.PlacedByUserId == StorefrontCheckoutComposer.StorefrontGuestActorId)
            ownedByActor = false;

        var ownedByGuest = false;
        if (!ownedByActor && !string.IsNullOrWhiteSpace(guestSecret))
        {
            try
            {
                var cart = await carts.GetCartAsync(
                    checkout.CartId, new CartAccess(null, guestSecret), cancellationToken);
                ownedByGuest = cart is not null;
            }
            catch (InvalidOperationException)
            {
                ownedByGuest = false;
            }
        }

        if (!ownedByActor && !ownedByGuest)
            return new SemanticError(FulfillmentErrorCodes.CustomerOrderMissing);

        return null;
    }

    private static Guid? ResolveCustomerActor(HttpRequest request, CurrentAuthenticatedSession session, IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } authenticated)
            return authenticated;

        var isDevSeam = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        if (!isDevSeam) return null;

        if (request.Headers.TryGetValue("X-Tooba-Dev-Actor-User-Id", out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
            return devActor;

        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }

    private static string? ReadGuestSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Tooba-Guest-Secret", out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString();
        return null;
    }
}
