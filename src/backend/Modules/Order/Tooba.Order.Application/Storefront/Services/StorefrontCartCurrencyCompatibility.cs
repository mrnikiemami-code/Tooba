using Tooba.Cart.Contracts;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>
/// Order boundary currency compatibility for a Cart that is already multi-currency capable.
/// Order multi-currency is explicitly deferred, so until its dedicated wave the Order/Checkout
/// boundary accepts ONLY a cart whose lines agree on exactly one distinct non-empty quoted currency.
/// The sole line currency is the effective Order/Checkout currency. <c>Cart.DefaultCurrency</c> is a
/// default-selection input for new Cart lines and is NEVER Order transaction authority here.
/// This type carries no shipping, pricing, reservation or payment policy; it only fails closed.
/// </summary>
public static class StorefrontCartCurrencyCompatibility
{
    /// <summary>
    /// Effective currency of a cart whose lines all share one non-empty currency.
    /// Mixed or currency-less carts fail closed with the typed stable Order error.
    /// </summary>
    public static string ResolveSoleCurrency(CartPage cart)
    {
        ArgumentNullException.ThrowIfNull(cart);
        return ResolveSoleCurrency(cart.Lines.Select(line => line.Currency));
    }

    /// <summary>
    /// Effective currency of a persisted Cart snapshot whose lines all share one non-empty
    /// <see cref="CartLineSnapshot.QuotedCurrency"/>. Mixed or currency-less carts fail closed.
    /// </summary>
    public static string ResolveSoleCurrency(CartSnapshot cart)
    {
        ArgumentNullException.ThrowIfNull(cart);
        return ResolveSoleCurrency(cart.Lines.Select(line => line.QuotedCurrency));
    }

    /// <summary>
    /// Sole line currency plus the matching per-currency Cart total.
    /// The total is never computed by summing unlike currencies: only the single group that matches
    /// the sole line currency is read, and any disagreement between lines and totals fails closed.
    /// </summary>
    public static (string Currency, decimal SubtotalExclusiveOfTax) ResolveSoleCurrencyAndSubtotal(CartPage cart)
    {
        var currency = ResolveSoleCurrency(cart);
        var matching = cart.TotalsByCurrency
            .Where(total => string.Equals(total.Currency.Trim(), currency, StringComparison.Ordinal))
            .ToArray();
        if (matching.Length != cart.TotalsByCurrency.Count)
        {
            // A totals entry in another currency would require a cross-currency sum to be meaningful.
            throw MixedCurrency();
        }

        return (currency, matching.Length == 0 ? 0m : matching[0].SubtotalExclusiveOfTax);
    }

    private static string ResolveSoleCurrency(IEnumerable<string?> lineCurrencies)
    {
        var distinct = lineCurrencies
            .Select(currency => currency?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (distinct.Length != 1 || distinct[0].Length == 0)
        {
            // Zero usable currencies (missing truth) and multiple currencies both fail closed.
            throw MixedCurrency();
        }

        return distinct[0];
    }

    private static StorefrontOrderException MixedCurrency() =>
        new(StorefrontOrderErrors.CheckoutMultiCurrencyNotSupported);
}
