using Tooba.BuildingBlocks;
using Tooba.Host;
using Tooba.Host.Configuration;
using Tooba.Persistence;

namespace Tooba.Host.Outbox;

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
