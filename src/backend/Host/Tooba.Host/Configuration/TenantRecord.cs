using Tooba.BuildingBlocks;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Host.Configuration;

/// <summary>
/// رکورد immutable پس از اعتبارسنجی پیکربندی؛ مبنای resolve درخواست.
/// </summary>
internal sealed class TenantRecord
{
    /// <summary>
    /// هویت پایدار Tenant.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    /// نام نمایشی.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// وضعیت عملیاتی پس از parse.
    /// </summary>
    public required TenantStatus Status { get; init; }

    /// <summary>
    /// مرجع اتصال فروشگاه.
    /// </summary>
    public required ConnectionReference ConnectionReference { get; init; }

    /// <summary>
    /// ارجاع تم.
    /// </summary>
    public string? ThemeReference { get; init; }

    /// <summary>
    /// ارجاع بازار پیش‌فرض.
    /// </summary>
    public string? DefaultMarketReference { get; init; }

    /// <summary>
    /// زمینهٔ تجارت مؤثر همین فروشگاه. اگر تنظیم نشده باشد، DefaultMarketReference بازار را تأمین می‌کند و ارز/کانال تهی می‌مانند.
    /// </summary>
    public StoreCommerceContext StoreCommerce { get; init; } = new(null, null, null);

    /// <summary>
    /// دامنهٔ اصلی نرمال‌شده در صورت وجود.
    /// </summary>
    public string? PrimaryDomain { get; init; }

    /// <summary>
    /// Hostهای نرمال‌شدهٔ این Tenant.
    /// </summary>
    public required IReadOnlyList<string> Hosts { get; init; }
}
