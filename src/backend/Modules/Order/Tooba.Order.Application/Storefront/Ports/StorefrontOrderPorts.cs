using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Storefront.Ports;

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
    /// Throws typed storefront error when saved-address requires auth outside Dev/Testing.
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
    Task EnsureCheckoutActorAsync(CancellationToken cancellationToken);
}

/// <summary>Order-owned shipping draft persistence.</summary>
public interface IStorefrontShippingDraftStore
{
    Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken);
    Task SaveAsync(CartShippingDraft draft, bool isNew, CancellationToken cancellationToken);
}

/// <summary>Order-owned pending-payment checkout reads/writes.</summary>
public interface IStorefrontPendingCheckoutStore
{
    Task<IReadOnlyList<CheckoutGroup>> ListOwnedByUserAsync(Guid userId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<CheckoutGroup>> ListByCheckoutIdsAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken);
    Task<CheckoutGroup?> GetWithSellerOrdersAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task HidePendingCardAsync(Guid checkoutId, Guid? ownerUserId, Guid? guestCartId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<HashSet<Guid>> LoadHiddenCheckoutIdsAsync(
        IReadOnlyList<Guid> checkoutIds,
        Guid? ownerUserId,
        IReadOnlyList<Guid> guestCartIds,
        CancellationToken cancellationToken);
}
