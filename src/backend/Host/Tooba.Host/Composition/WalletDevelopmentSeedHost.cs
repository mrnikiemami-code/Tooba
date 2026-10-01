using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin.Development;
using Tooba.Wallet.Infrastructure.Development;

namespace Tooba.Host.Composition;

/// <summary>
/// Thin Host composition seam for Wallet Development seed: binds store-alpha CommerceContext
/// and Admin demo actor, then delegates migrate/seed to Wallet.Infrastructure.
/// </summary>
internal static class WalletDevelopmentSeedHost
{
    /// <summary>Runs only when store-alpha is Active and Admin actor is ready.</summary>
    public static async Task ApplyAsync(IServiceProvider root)
    {
        await using var scope = root.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
            return;

        var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(new CommerceContext(
            new EditionContext(registry.Edition, registry.DeploymentId),
            new TenantContext(
                tenant.TenantId,
                tenant.Status,
                tenant.ConnectionReference,
                tenant.DisplayName,
                tenant.ThemeReference,
                tenant.DefaultMarketReference,
                tenant.Hosts[0],
                tenant.PrimaryDomain),
            tenant.ConnectionReference,
            "wallet-dev-seed"));

        await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        var admin = AdminDevActorBootstrap.Snapshot;
        if (admin is null)
            return;

        await WalletDevelopmentSeedBootstrap.ApplyAsync(
            provider,
            WalletDevelopmentSeedBootstrap.DemoCustomerActorUserId,
            admin.ActorUserId,
            CancellationToken.None);
    }
}
