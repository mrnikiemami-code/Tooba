using Tooba.BuildingBlocks;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Host.Configuration;

/// <summary>
/// تصویر فقط‌خواندنی control plane پیکربندی‌شده برای یک فرآیند. منبع تولید Tenant نیست.
/// </summary>
internal sealed class ControlPlaneRegistry
{
    /// <summary>
    /// Edition قفل‌شدهٔ فرآیند.
    /// </summary>
    public required ToobaEdition Edition { get; init; }

    /// <summary>
    /// برچسب استقرار.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// مرجع اتصال marketplace؛ در Single-Store تهی است.
    /// </summary>
    public ConnectionReference? MarketplaceConnectionReference { get; init; }

    /// <summary>
    /// زمینهٔ تجارت مؤثر در سطح deployment (مسیر Marketplace). در Single-Store از Tenant خوانده می‌شود.
    /// </summary>
    public StoreCommerceContext DeploymentStoreCommerce { get; init; } = new(null, null, null);

    /// <summary>
    /// نگاشت Host نرمال‌شده → Tenant. کلید هویت نیست.
    /// </summary>
    public required IReadOnlyDictionary<string, TenantRecord> Hosts { get; init; }

    /// <summary>
    /// نگاشت TenantId → رکورد.
    /// </summary>
    public required IReadOnlyDictionary<string, TenantRecord> Tenants { get; init; }
}
