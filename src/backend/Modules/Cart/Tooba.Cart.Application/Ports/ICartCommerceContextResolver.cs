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
/// Reads the effective storefront commerce context from the canonical commerce/storefront
/// configuration already present in the platform. Unresolvable context fails closed with a
/// stable Cart error code instead of a hardcoded fallback.
/// </summary>
public interface ICartCommerceContextResolver
{
    /// <summary>
    /// Resolves the effective commerce context for the current commerce context.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Stable <c>cart.commerce.*</c> code when market or currency is not configured.
    /// </exception>
    CartCommerceContext Resolve();
}
