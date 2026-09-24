using Tooba.Cart.Application.Errors;
using Tooba.Cart.Domain.Aggregates;
using Tooba.Cart.Domain.Entities;
using Tooba.Pricing.Contracts;

namespace Tooba.Cart.Infrastructure.Directories;

/// <summary>
/// Cart-owned line-currency selection rules. Pricing remains the quote authority; these helpers only
/// decide which currency selector is passed into Pricing for a given line, and enforce that an
/// already-quoted line keeps its own currency instead of inheriting a cart-level default.
/// </summary>
internal static class CartLineCurrency
{
    /// <summary>
    /// Selected currency for a NEW line: the explicitly requested currency when supplied, otherwise
    /// the cart's default selection. The requested value is a selector input, not pricing authority.
    /// </summary>
    public static string ForNewLine(ShoppingCart cart, string? requestedCurrency) =>
        string.IsNullOrWhiteSpace(requestedCurrency)
            ? cart.DefaultCurrency
            : CurrencyCode.Parse(requestedCurrency.Trim()).Value;

    /// <summary>
    /// Authoritative currency of an EXISTING quoted line. Missing truth fails closed; it is never
    /// replaced by the cart default or by a different requested currency.
    /// </summary>
    public static string RequireForExistingLine(CartLine line)
    {
        if (string.IsNullOrWhiteSpace(line.QuotedCurrency))
        {
            throw new InvalidOperationException(CartErrorCodes.LineCurrencyMissing);
        }

        return line.QuotedCurrency;
    }
}
