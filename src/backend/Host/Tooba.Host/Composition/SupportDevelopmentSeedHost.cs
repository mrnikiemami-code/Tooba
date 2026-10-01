using Microsoft.Extensions.DependencyInjection;
using Tooba.AccessControl.Application.Development.Seller;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin.Development;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Support.Infrastructure.Development;

namespace Tooba.Host.Composition;

/// <summary>
/// Thin Host composition seam for Support Development seed: binds store-alpha CommerceContext,
/// Admin/Seller demo actors, and AccessControl bootstrap tuples, then delegates migrate/seed
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
        var sellerDevContexts = provider.GetRequiredService<ISellerDevContextStore>();
        await sellerDevContexts.EnsureAsync(CancellationToken.None);

        var seller = sellerDevContexts.Current;
        var admin = AdminDevActorBootstrap.Snapshot;
        if (seller is null || admin is null)
            return;

        var access = provider.GetRequiredService<IAccessControlDirectory>();
        await access.EnsureBootstrapAsync(
            admin.ActorUserId,
            [seller.ActorA.SellerPartyId],
            tenant.TenantId.Value,
            CancellationToken.None);
        await access.SyncUserCapabilityTuplesAsync(
            seller.ActorA.ActorUserId,
            new AccessOwnerScope(
                AccessOwnerScopeKind.Seller,
                seller.ActorA.SellerPartyId,
                tenant.TenantId.Value),
            CancellationToken.None);

        await SupportDevelopmentSeedBootstrap.ApplyAsync(
            provider,
            StorefrontGuestActor.ActorId,
            seller.ActorA.SellerPartyId,
            seller.ActorA.ActorUserId,
            admin.ActorUserId,
            CancellationToken.None);
    }
}
