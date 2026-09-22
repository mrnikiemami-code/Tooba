using Tooba.Cart.Application.Models;
using Tooba.Cart.Contracts;

namespace Tooba.Cart.Application.Ports;

/// <summary>
/// Cart presentation reads for checkout-adjacent Host composers (compile seam).
/// HTTP ownership remains in Cart.Endpoints → MediatR.
/// </summary>
public interface ICartPresentationGateway
{
    /// <summary>Loads and presents a cart after access checks. Null when the cart id is unknown.</summary>
    Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken);

    /// <summary>
    /// Ownership probe for committed checkout: invalid guest secret yields null (not an exception).
    /// </summary>
    Task<CartPage?> TryGetForOwnershipAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken);
}
