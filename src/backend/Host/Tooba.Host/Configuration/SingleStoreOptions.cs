
namespace Tooba.Host.Configuration;

/// <summary>
/// فهرست Tenantهای Single-Store در پیکربندی محلی.
/// </summary>
internal sealed class SingleStoreOptions
{
    /// <summary>
    /// رکوردهای Tenant؛ هر کدام حداقل یک Host نرمال‌شده نیاز دارند.
    /// </summary>
    public List<TenantRecordOptions> Tenants { get; set; } = [];
}
