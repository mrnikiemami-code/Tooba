using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Application.Ports;

/// <summary>
/// Effective storefront commerce context consumed by Cart when it must create a cart without
/// an existing cart to inherit from. Cart consumes this context; it never becomes the policy
/// authority for Market, Currency, or SalesChannel and never trusts raw HTTP strings.
/// </summary>
/// <param name="Market">Effective commercial market reference.</param>
/// <param name="Currency">Effective ISO currency code.</param>
/// <param name="Channel">Effective sales channel.</param>
public sealed record CartCommerceContext(string Market, string Currency, SalesChannel Channel);

/// <summary>
/// Reads the effective storefront commerce context that the platform control plane already
/// resolved for the current request or worker. Cart consumes this context; it never becomes the
/// policy authority for Market, Currency, or SalesChannel, owns no fallback defaults, and never
/// trusts raw HTTP strings.
/// </summary>
public interface ICartCommerceContextResolver
{
    /// <summary>
    /// Resolves the effective commerce context authoritative for the current storefront/store.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Stable <c>cart.commerce.*</c> code when the platform-boundary context is absent or does not
    /// carry a market, currency, or channel. Cart never invents a default.
    /// </exception>
    CartCommerceContext Resolve();
}
