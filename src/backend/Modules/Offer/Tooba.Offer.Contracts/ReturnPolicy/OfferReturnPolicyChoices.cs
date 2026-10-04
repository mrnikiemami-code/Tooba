namespace Tooba.Offer.Contracts.ReturnPolicy;

/// <summary>
/// انتخاب canonical سیاست روی Offer.
/// </summary>
public static class OfferReturnPolicyChoices
{
    /// <summary>پیروی از پیش‌فرض فروشگاه.</summary>
    public const string Default = "Default";

    /// <summary>مهلت اختصاصی.</summary>
    public const string Custom = "Custom";

    /// <summary>غیرقابل مرجوعی.</summary>
    public const string NonReturnable = "NonReturnable";

    /// <summary>نرمال‌سازی ورودی.</summary>
    public static string Normalize(string? value) =>
        value?.Trim() switch
        {
            Custom => Custom,
            NonReturnable => NonReturnable,
            _ => Default,
        };
}
