
namespace Tooba.Host.Configuration;

/// <summary>
/// شکل خام یک Tenant در پیکربندی قبل از نرمال‌سازی Host.
/// </summary>
internal sealed class TenantRecordOptions
{
    /// <summary>
    /// هویت پایدار؛ hostname نیست.
    /// </summary>
    public string TenantId { get; set; } = "";

    /// <summary>
    /// نام نمایشی اختیاری.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Active / Disabled / Suspended.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// مرجع اتصال فروشگاه این Tenant.
    /// </summary>
    public string ConnectionReference { get; set; } = "";

    /// <summary>
    /// ارجاع تم اختیاری.
    /// </summary>
    public string? ThemeReference { get; set; }

    /// <summary>
    /// ارجاع بازار پیش‌فرض؛ با Locale یکی نیست.
    /// </summary>
    public string? DefaultMarketReference { get; set; }

    /// <summary>
    /// بازار می‌تواند از DefaultMarketReference ارث ببرد؛ ارز و کانال فروش نیازمند مقدار صریح StoreCommerce در همین Tenant هستند.
    /// </summary>
    public StoreCommerceOptions? StoreCommerce { get; set; }

    /// <summary>
    /// دامنهٔ اصلی در صورت وجود.
    /// </summary>
    public string? PrimaryDomain { get; set; }

    /// <summary>
    /// Hostهای مجاز برای routing به این Tenant.
    /// </summary>
    public List<string> Hosts { get; set; } = [];
}
