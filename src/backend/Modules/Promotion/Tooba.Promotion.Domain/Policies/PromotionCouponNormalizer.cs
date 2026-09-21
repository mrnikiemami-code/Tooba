namespace Tooba.Promotion.Domain.Policies;

/// <summary>
/// نرمال‌سازی کد کوپن. داشتن کد به‌تنهایی مجوز اعمال نیست.
/// </summary>
public static class PromotionCouponNormalizer
{
    /// <summary>
    /// فاصله‌ها را می‌زداید و حروف راInvariant بزرگ می‌کند تا مقایسه قطعی باشد.
    /// </summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return string.Join(
            "",
            code.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
