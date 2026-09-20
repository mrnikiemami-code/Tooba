using Tooba.BuildingBlocks;

namespace Tooba.Tax.Domain;

/// <summary>
/// گرد کردن قطعی مبلغ مالیات طبق مقیاس ارز. ممیز شناور نیست.
/// </summary>
public static class TaxRounding
{
    /// <summary>
    /// IRR بدون اعشار؛ بقیه دو رقم. Midpoint AwayFromZero.
    /// </summary>
    public static decimal Round(decimal amount, string currency)
    {
        var scale = string.Equals(currency, "IRR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "JPY", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "KRW", StringComparison.OrdinalIgnoreCase)
            ? 0
            : 2;
        return Round(amount, currency, QuantityRoundingMode.Nearest);
    }

    /// <summary>گرد کردن با GlobalRoundingMode.</summary>
    public static decimal Round(decimal amount, string currency, QuantityRoundingMode mode)
    {
        var scale = string.Equals(currency, "IRR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "JPY", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "KRW", StringComparison.OrdinalIgnoreCase)
            ? 0
            : 2;
        return FinancialRounder.Round(amount, scale, mode);
    }
}
