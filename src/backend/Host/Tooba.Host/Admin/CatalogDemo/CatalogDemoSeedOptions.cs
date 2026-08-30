namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>
/// گزینه‌های ایمن دانه/بازنشانی Catalog Demo (TB-P07-T033). پیش‌فرض fail-closed.
/// </summary>
public sealed class CatalogDemoSeedOptions
{
    /// <summary>بخش پیکربندی.</summary>
    public const string SectionName = "Tooba:CatalogDemo";

    /// <summary>
    /// اجازهٔ صریح reset+seed. بدون این پرچم هیچ جهشی انجام نمی‌شود.
    /// </summary>
    public bool AllowResetAndSeed { get; set; }

    /// <summary>
    /// اجرای bootstrapهای قدیمی Development (Storefront/ProductWorkspace/AttributeSchema/ACC).
    /// پیش‌فرض false تا پس از reset دوباره آلوده نشوند.
    /// </summary>
    public bool RunLegacyBootstraps { get; set; }
}
