using Tooba.Cart.Contracts;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Storefront.Ports;

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
