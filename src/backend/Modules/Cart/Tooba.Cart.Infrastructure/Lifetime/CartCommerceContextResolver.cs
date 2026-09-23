using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Models;
using Tooba.Cart.Application.Ports;

namespace Tooba.Cart.Infrastructure.Lifetime;

/// <summary>
/// Reads the effective storefront commerce context from the canonical commerce context already
/// resolved for the request, plus Cart-owned fallback configuration for market, currency, and channel.
/// Cart never trusts raw HTTP values and never hardcodes market or currency literals.
/// </summary>
public sealed class CartCommerceContextResolver : ICartCommerceContextResolver
{
    private readonly ICurrentCommerceContext _commerce;
    private readonly CartCommerceDefaultsOptions _defaults;

    /// <summary>Binds the canonical commerce context and Cart-owned fallback defaults.</summary>
    /// <param name="commerce">Canonical per-request commerce context.</param>
    /// <param name="defaults">Cart-owned commerce fallback options.</param>
    public CartCommerceContextResolver(
        ICurrentCommerceContext commerce,
        IOptions<CartCommerceDefaultsOptions> defaults)
    {
        _commerce = commerce;
        _defaults = defaults.Value;
    }

    /// <inheritdoc />
    public CartCommerceContext Resolve()
    {
        var tenantMarket = _commerce.Current?.Tenant?.DefaultMarketReference;
        var market = string.IsNullOrWhiteSpace(tenantMarket) ? _defaults.DefaultMarket : tenantMarket;
        if (string.IsNullOrWhiteSpace(market))
        {
            throw new InvalidOperationException("cart.commerce.market_unconfigured");
        }

        var currency = _defaults.DefaultCurrency;
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidOperationException("cart.commerce.currency_unconfigured");
        }

        return new CartCommerceContext(market.Trim(), currency.Trim(), _defaults.DefaultSalesChannel);
    }
}
