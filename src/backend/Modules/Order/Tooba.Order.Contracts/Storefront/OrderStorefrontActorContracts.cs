using Tooba.Cart.Contracts;

namespace Tooba.Order.Contracts.Storefront;

/// <summary>Thin Host-facing actor/session seam for storefront Order surfaces (no business rules).</summary>
public interface IOrderStorefrontActor
{
    /// <summary>Canonical guest actor id for storefront placement.</summary>
    Guid GuestActorId { get; }

    /// <summary>True when the current HTTP session is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Authenticated user id when present.</summary>
    Guid? AuthenticatedUserId { get; }

    /// <summary>
    /// Placement actor: session → Dev/Testing header → guest.
    /// Throws stable semantic auth fault when saved-address requires auth outside Dev/Testing.
    /// </summary>
    Guid ResolvePlacementActor(bool usingSavedAddress);

    /// <summary>List/cancel actor: session or Dev/Testing header; null means guest-proof mode.</summary>
    Guid? TryResolveListActor();

    /// <summary>Builds CartAccess from session + optional guest secret.</summary>
    CartAccess BuildCartAccess(string? guestSecret);
}

/// <summary>Host checkout identity policy gate (EnsureCheckoutActor) without Catalog leakage.</summary>
public interface IOrderStorefrontCheckoutIdentityGate
{
    /// <summary>Enforces checkout actor policy for the current request.</summary>
    Task EnsureCheckoutActorAsync(CancellationToken cancellationToken);
}
