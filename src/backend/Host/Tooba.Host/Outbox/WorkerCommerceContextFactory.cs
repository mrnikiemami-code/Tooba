using Tooba.BuildingBlocks;
using Tooba.Host;
using Tooba.Persistence;

namespace Tooba.Host.Outbox;

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
    /// Rebuilds worker commerce context from the poll target; HTTP Host headers are not read.
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
            throw new InvalidOperationException("Worker commerce context could not be reconstructed from registry.");
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
