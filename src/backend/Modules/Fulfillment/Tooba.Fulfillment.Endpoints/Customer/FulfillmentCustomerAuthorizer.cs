using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Fulfillment.Endpoints.Customer;

/// <summary>
/// مالکیت checkout مشتری/مهمان برای لیست Fulfillment. هویت Actor از درز عمومی پلتفرم و
/// مالکیت از قراردادهای Order/Cart می‌آید؛ ماژول به Host و Order.Application وابسته نیست.
/// </summary>
public sealed class FulfillmentCustomerAuthorizer(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment,
    ICustomerCheckoutOwnershipReader ownership,
    ICartQueryGateway carts) : IFulfillmentCustomerAuthorizer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";
    private const string GuestSecretHeader = "X-Tooba-Guest-Secret";

    /// <inheritdoc />
    public async Task<SemanticError?> EnsureCanViewCheckoutAsync(
        HttpContext httpContext, Guid checkoutId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var actor = ResolveCustomerActor(httpContext, currentUser, environment);
        var guestSecret = ReadGuestSecret(httpContext);
        if (actor is null && string.IsNullOrWhiteSpace(guestSecret))
            return new SemanticError(FulfillmentErrorCodes.CustomerActorMissing);

        var checkout = await ownership.GetAsync(checkoutId, cancellationToken);
        if (checkout is null)
            return new SemanticError(FulfillmentErrorCodes.CustomerOrderMissing);

        var ownedByActor = actor is not null && checkout.PlacedByUserId == actor.Value;
        if (checkout.PlacedByUserId == StorefrontGuestActor.ActorId)
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

    private static Guid? ResolveCustomerActor(
        HttpContext httpContext,
        ICurrentAuthenticatedUser currentUser,
        IHostEnvironment environment)
    {
        if (currentUser.IsAuthenticated && currentUser.UserId is { } authenticated)
            return authenticated;

        var isDevSeam = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        if (!isDevSeam) return null;

        if (httpContext.Request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
            return devActor;

        return StorefrontGuestActor.ActorId;
    }

    private static string? ReadGuestSecret(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(GuestSecretHeader, out var header)
            && !string.IsNullOrWhiteSpace(header))
            return header.ToString();
        return null;
    }
}
