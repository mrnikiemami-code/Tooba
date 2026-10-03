namespace Tooba.Party.Domain.Enums;

/// <summary>
/// کد قابلیت تجاری گسترش‌پذیر روی سازمان. یک سازمان می‌تواند چند قابلیت داشته باشد.
/// </summary>
public static class PartyCapabilityCodes
{
    /// <summary>
    /// درز فروشنده؛ در این foundation فعال‌سازی onboarding نیست.
    /// </summary>
    public const string Seller = "seller";

    /// <summary>
    /// درز آژانس.
    /// </summary>
    public const string Agency = "agency";

    /// <summary>
    /// درز خریدار سازمانی.
    /// </summary>
    public const string CorporateBuyer = "corporate_buyer";
}
