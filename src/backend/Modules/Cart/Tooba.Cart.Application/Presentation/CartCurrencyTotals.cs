using Tooba.Cart.Contracts;

namespace Tooba.Cart.Application.Presentation;

/// <summary>
/// Cart line currency arithmetic. Only unlike-currency separation lives here; no FX and no pricing.
/// </summary>
public static class CartCurrencyTotals
{
    /// <summary>
    /// Groups tax-exclusive line amounts by line currency. Unlike currencies are never summed into
    /// one scalar. Ordering is deterministic ordinal on the currency code.
    /// </summary>
    public static IReadOnlyList<CartCurrencyTotal> Group(
        IEnumerable<(string Currency, decimal? LineAmountExclusiveOfTax)> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return lines
            .Select(item => (Currency: item.Currency.Trim(), Amount: item.LineAmountExclusiveOfTax ?? 0m))
            .Where(item => item.Currency.Length > 0)
            .GroupBy(item => item.Currency, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CartCurrencyTotal(group.Key, group.Sum(item => item.Amount)))
            .ToArray();
    }
}
