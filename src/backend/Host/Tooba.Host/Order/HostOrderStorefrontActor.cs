using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Host;
using Tooba.Host.Storefront;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Host.Order;

/// <summary>
/// Thin Host adapter: session / Dev-Testing actor header / guest — no business decisions.
/// </summary>
internal sealed class HostOrderStorefrontActor(
    CurrentAuthenticatedSession session,
    IHostEnvironment environment,
    IHttpContextAccessor http) : IOrderStorefrontActor
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    public Guid GuestActorId => StorefrontCheckoutService.StorefrontGuestActorId;

    public bool IsAuthenticated => session.IsAuthenticated;

    public Guid? AuthenticatedUserId =>
        session.IsAuthenticated && session.UserId is Guid userId && userId != Guid.Empty
            ? userId
            : null;

    public Guid ResolvePlacementActor(bool usingSavedAddress)
    {
        if (AuthenticatedUserId is Guid userId)
        {
            return userId;
        }

        var request = http.HttpContext?.Request;
        var isDevSeam = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        if (isDevSeam
            && request is not null
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var headerActor)
            && headerActor != Guid.Empty)
        {
            return headerActor;
        }

        if (usingSavedAddress && !isDevSeam)
        {
            throw new StorefrontOrderException(
                StorefrontOrderErrors.CheckoutAuthenticationRequired,
                "saved address requires authenticated session outside Dev/Testing");
        }

        return GuestActorId;
    }

    public Guid? TryResolveListActor()
    {
        if (AuthenticatedUserId is Guid userId)
        {
            return userId;
        }

        var request = http.HttpContext?.Request;
        var isDevSeam = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        if (isDevSeam
            && request is not null
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var headerActor)
            && headerActor != Guid.Empty)
        {
            return headerActor;
        }

        return null;
    }

    public CartAccess BuildCartAccess(string? guestSecret) =>
        new(AuthenticatedUserId, guestSecret);
}

/// <summary>Thin Host wrapper over CheckoutIdentityGate.</summary>
internal sealed class HostOrderStorefrontCheckoutIdentityGate(CheckoutIdentityGate gate)
    : IOrderStorefrontCheckoutIdentityGate
{
    public Task EnsureCheckoutActorAsync(CancellationToken cancellationToken) =>
        gate.EnsureCheckoutActorAsync(cancellationToken);
}
