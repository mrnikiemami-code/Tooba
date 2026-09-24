using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Host;

/// <summary>
/// فهرست پایگاه‌های poll از control plane پیکربندی. Host درخواست در این فهرست نیست.
/// </summary>
internal sealed class ConfiguredOutboxPollTargetSource : IOutboxPollTargetSource
{
    private readonly ControlPlaneRegistry _registry;

    /// <summary>
    /// منبع اهداف را به registry فرآیند وصل می‌کند.
    /// </summary>
    public ConfiguredOutboxPollTargetSource(ControlPlaneRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public IReadOnlyList<OutboxPollTarget> GetTargets()
    {
        if (_registry.Edition == ToobaEdition.Marketplace)
        {
            if (_registry.MarketplaceConnectionReference is not { } marketplace)
            {
                return [];
            }

            return
            [
                new OutboxPollTarget(
                    ToobaEdition.Marketplace,
                    TenantId: null,
                    marketplace,
                    _registry.DeploymentId),
            ];
        }

        if (_registry.Edition != ToobaEdition.SingleStore)
        {
            return [];
        }

        return _registry.Tenants.Values
            .Where(tenant => tenant.Status == TenantStatus.Active)
            .Select(tenant => new OutboxPollTarget(
                ToobaEdition.SingleStore,
                tenant.TenantId.Value,
                tenant.ConnectionReference,
                _registry.DeploymentId))
            .ToArray();
    }
}

/// <summary>
/// بازسازی <see cref="CommerceContext"/> برای کارگر از ردیف Outbox و registry. Host هدر خوانده نمی‌شود.
/// </summary>
internal sealed class WorkerCommerceContextFactory : IWorkerCommerceContextFactory
{
    private readonly ControlPlaneRegistry _registry;

    /// <summary>
    /// کارخانه را به registry پیکربندی وصل می‌کند.
    /// </summary>
    public WorkerCommerceContextFactory(ControlPlaneRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// زمینهٔ handler را از TenantId/Edition ذخیره‌شده می‌سازد. جعل Host روی درخواست HTTP بی‌اثر است.
    /// </summary>
    /// <param name="message">ردیف claimشده.</param>
    /// <param name="traceId">همبستگی تله‌متری این تلاش.</param>
    public CommerceContext FromOutbox(OutboxMessage message, string traceId)
    {
        var edition = _registry.Edition;
        var editionContext = new EditionContext(edition, _registry.DeploymentId);

        if (edition == ToobaEdition.Marketplace)
        {
            var marketplace = _registry.MarketplaceConnectionReference
                ?? throw new InvalidOperationException("Marketplace outbox worker has no connection reference.");
            return new CommerceContext(
                editionContext,
                Tenant: null,
                marketplace,
                traceId);
        }

        if (string.IsNullOrWhiteSpace(message.TenantId)
            || !_registry.Tenants.TryGetValue(message.TenantId, out var record)
            || record.Status != TenantStatus.Active)
        {
            throw new InvalidOperationException("Outbox tenant could not be reconstructed from registry.");
        }

        var resolvedHost = record.PrimaryDomain ?? record.Hosts[0];
        var tenant = new TenantContext(
            record.TenantId,
            record.Status,
            record.ConnectionReference,
            record.DisplayName,
            record.ThemeReference,
            record.DefaultMarketReference,
            resolvedHost,
            record.PrimaryDomain);

        return new CommerceContext(
            editionContext,
            tenant,
            record.ConnectionReference,
            traceId);
    }

    /// <summary>
    /// زمینهٔ کارگر انقضای سبد را از هدف poll می‌سازد؛ هدر HTTP خوانده نمی‌شود.
    /// </summary>
    public CommerceContext FromPollTarget(OutboxPollTarget target, string traceId)
    {
        var editionContext = new EditionContext(target.Edition, target.DeploymentId);
        if (target.Edition == ToobaEdition.Marketplace)
        {
            return new CommerceContext(
                editionContext,
                Tenant: null,
                target.ConnectionReference,
                traceId);
        }

        if (string.IsNullOrWhiteSpace(target.TenantId)
            || !_registry.Tenants.TryGetValue(target.TenantId, out var record)
            || record.Status != TenantStatus.Active)
        {
            throw new InvalidOperationException("زمینهٔ کارگر انقضای سبد از registry بازسازی نشد.");
        }

        var resolvedHost = record.PrimaryDomain ?? record.Hosts[0];
        var tenant = new TenantContext(
            record.TenantId,
            record.Status,
            record.ConnectionReference,
            record.DisplayName,
            record.ThemeReference,
            record.DefaultMarketReference,
            resolvedHost,
            record.PrimaryDomain);
        return new CommerceContext(
            editionContext,
            tenant,
            record.ConnectionReference,
            traceId);
    }
}

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
