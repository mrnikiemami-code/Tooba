using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Domain.Policies;

/// <summary>
/// گرد کردن قطعی مبلغ تخفیف طبق مقیاس ارز. ممیز شناور نیست.
/// </summary>
public static class PromotionRounding
{
    /// <summary>
    /// IRR/JPY/KRW بدون اعشار؛ بقیه دو رقم. Midpoint AwayFromZero.
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

    /// <summary>گرد کردن با GlobalRoundingMode و دقت ارز.</summary>
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
