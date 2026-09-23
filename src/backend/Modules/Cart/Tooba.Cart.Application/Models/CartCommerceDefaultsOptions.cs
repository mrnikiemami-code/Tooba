namespace Tooba.Cart.Application.Models;

/// <summary>
/// Cart commerce defaults owned by Cart runtime configuration. Cart consumes the effective
/// storefront context; these values are only the Cart-side fallback knobs, never the policy
/// authority, and never a hardcoded literal inside Cart handlers.
/// </summary>
public sealed class CartCommerceDefaultsOptions
{
    /// <summary>نام بخش پیکربندی زیر <c>Cart</c>.</summary>
    public const string SectionName = "Cart:CommerceDefaults";

    /// <summary>
    /// Effective market fallback when the commerce context does not carry a tenant market reference.
    /// Empty/null fails closed so an unconfigured store never invents a market.
    /// </summary>
    public string? DefaultMarket { get; set; }

    /// <summary>
    /// Effective currency fallback when the commerce context does not carry one.
    /// Empty/null fails closed so an unconfigured store never invents a currency.
    /// </summary>
    public string? DefaultCurrency { get; set; }

    /// <summary>Effective sales channel fallback for storefront cart creation.</summary>
    public Tooba.Offer.Contracts.Dtos.SalesChannel DefaultSalesChannel { get; set; } =
        Tooba.Offer.Contracts.Dtos.SalesChannel.Direct;
}
