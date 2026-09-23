using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Ports;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Infrastructure.Lifetime;

/// <summary>
/// Reads the effective storefront commerce context that the platform control plane already
/// resolved for the current request or worker. Cart consumes this context; it never owns or
/// invents Market, Currency, or SalesChannel defaults and never trusts raw HTTP values.
/// </summary>
public sealed class CartCommerceContextResolver : ICartCommerceContextResolver
{
    private readonly ICurrentCommerceContext _commerce;

    /// <summary>Binds the canonical per-request commerce context.</summary>
    /// <param name="commerce">Canonical commerce context resolved by the platform boundary.</param>
    public CartCommerceContextResolver(ICurrentCommerceContext commerce)
    {
        _commerce = commerce;
    }

    /// <inheritdoc />
    public CartCommerceContext Resolve()
    {
        var store = _commerce.Current?.StoreCommerce;
        if (store is null)
        {
            throw new InvalidOperationException("cart.commerce.context_unavailable");
        }

        var market = store.Market;
        if (string.IsNullOrWhiteSpace(market))
        {
            throw new InvalidOperationException("cart.commerce.market_unconfigured");
        }

        var currency = store.Currency;
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidOperationException("cart.commerce.currency_unconfigured");
        }

        var channel = ParseChannel(store.SalesChannel);
        return new CartCommerceContext(market.Trim(), currency.Trim(), channel);
    }

    /// <summary>
    /// نام پایدار کانال فروش را از مرز پلتفرم می‌خواند. نبود/نامعتبر یعنی resolve نشده و fail-closed می‌شود.
    /// </summary>
    private static SalesChannel ParseChannel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || !Enum.TryParse<SalesChannel>(raw.Trim(), ignoreCase: true, out var parsed))
        {
            throw new InvalidOperationException("cart.commerce.channel_unconfigured");
        }

        return parsed;
    }
}
