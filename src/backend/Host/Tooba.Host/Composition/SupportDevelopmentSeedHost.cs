using Microsoft.Extensions.DependencyInjection;
using Tooba.AccessControl.Contracts.Development;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin.Development;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Support.Infrastructure.Development;

namespace Tooba.Host.Composition;

/// <summary>
/// Thin Host composition seam for Support Development seed: binds store-alpha CommerceContext,
/// Admin demo actor, and AccessControl Contracts prelude, then delegates migrate/seed
/// to Support.Infrastructure.
/// </summary>
internal static class SupportDevelopmentSeedHost
{
    /// <summary>Runs only when store-alpha is Active and Admin/Seller actors are ready.</summary>
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
            "support-dev-seed"));

        await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        var admin = AdminDevActorBootstrap.Snapshot;
        if (admin is null)
            return;

        var prelude = provider.GetRequiredService<IAccessControlDevelopmentSeedPrelude>();
        var actors = await prelude.EnsureSupportSeedPrerequisitesAsync(
            admin.ActorUserId,
            tenant.TenantId.Value,
            CancellationToken.None);
        if (actors is null)
            return;

        await SupportDevelopmentSeedBootstrap.ApplyAsync(
            provider,
            StorefrontGuestActor.ActorId,
            actors.SellerPartyId,
            actors.SellerActorUserId,
            admin.ActorUserId,
            CancellationToken.None);
    }
}
