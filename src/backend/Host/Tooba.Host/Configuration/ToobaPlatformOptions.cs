using Tooba.BuildingBlocks;

namespace Tooba.Host.Configuration;

/// <summary>
/// بایندینگ پیکربندی بخش <c>Tooba</c>. این registry کنترل‌پلین تولید نیست؛ فقط bootstrap پیکربندی است.
/// کلیدهای dictionary اتصال نباید نویسهٔ <c>:</c> داشته باشند چون ASP.NET آن‌ها را تو در تو می‌کند.
/// </summary>
internal sealed class ToobaPlatformOptions
{
    /// <summary>
    /// نام بخش پیکربندی ریشه.
    /// </summary>
    public const string SectionName = "Tooba";

    /// <summary>
    /// Marketplace | SingleStore | Unset — یک فرآیند یک Edition.
    /// </summary>
    public string Edition { get; set; } = "Unset";

    /// <summary>
    /// برچسب استقرار برای تله‌متری؛ TenantId نیست.
    /// </summary>
    public string DeploymentId { get; set; } = "";

    /// <summary>
    /// IPهای proxy مورد اعتماد. خالی یعنی Forwarded Host اعمال نشود.
    /// </summary>
    public List<string> TrustedProxies { get; set; } = [];

    /// <summary>
    /// تنظیمات اختصاصی Marketplace.
    /// </summary>
    public MarketplaceOptions Marketplace { get; set; } = new();

    /// <summary>
    /// تنظیمات allowlist Single-Store.
    /// </summary>
    public SingleStoreOptions SingleStore { get; set; } = new();

    /// <summary>
    /// نقشهٔ ConnectionReference به رشتهٔ اتصال. مقدار رشته لاگ نشود.
    /// </summary>
    public PostgreSqlOptions PostgreSQL { get; set; } = new();

    /// <summary>
    /// زمینهٔ تجارت مؤثر فروشگاه در سطح deployment (مسیر Marketplace). در Single-Store از هر Tenant خوانده می‌شود.
    /// مالک این تنظیم کنترل‌پلین پلتفرم است؛ ماژول مصرف‌کننده صاحب پیش‌فرض تجاری نمی‌شود.
    /// </summary>
    public StoreCommerceOptions StoreCommerce { get; set; } = new();
}
