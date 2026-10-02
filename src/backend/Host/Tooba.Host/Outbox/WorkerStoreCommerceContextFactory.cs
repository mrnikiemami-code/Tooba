using Tooba.BuildingBlocks;
using Tooba.Host;
using Tooba.Host.Configuration;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Host.Outbox;

/// <summary>
/// انتخاب زمینهٔ تجارت مؤثر فروشگاه برای کارگر از registry. این adapter موقت پلتفرم در Foundation
/// Phase است: فقط StoreCommerce سطح deployment (Marketplace) یا رکورد Tenant فعال (Single-Store) را
/// انتخاب می‌کند و هیچ پیش‌فرض/نرمال‌سازی Market/Currency/SalesChannel ندارد.
/// </summary>
internal sealed class WorkerStoreCommerceContextFactory : IWorkerStoreCommerceContextFactory
{
    private readonly ControlPlaneRegistry _registry;

    /// <summary>
    /// factory را به registry پیکربندی وصل می‌کند.
    /// </summary>
    public WorkerStoreCommerceContextFactory(ControlPlaneRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public StoreCommerceContext FromTarget(ToobaEdition edition, string? tenantId)
    {
        if (edition == ToobaEdition.Marketplace)
        {
            return _registry.DeploymentStoreCommerce;
        }

        if (string.IsNullOrWhiteSpace(tenantId)
            || !_registry.Tenants.TryGetValue(tenantId, out var record)
            || record.Status != TenantStatus.Active)
        {
            throw new InvalidOperationException("Worker store commerce context could not be reconstructed from registry.");
        }

        return record.StoreCommerce;
    }
}
