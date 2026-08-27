using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;
using Tooba.Host.Storefront;
using Tooba.Support.Infrastructure;
using Tooba.Support.Infrastructure.Persistence;

namespace Tooba.Host.Support;

/// <summary>اعمال دانهٔ توسعه Support روی scope با CommerceContext.</summary>
internal static class SupportDevelopmentSeedHost
{
    /// <summary>دانه را فقط در Development و با Actor/Seller آماده اجرا می‌کند.</summary>
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

        var db = provider.GetRequiredService<SupportDbContext>();
        await db.Database.MigrateAsync();

        await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        await SellerDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);

        var seller = SellerDevActorBootstrap.Snapshot;
        var admin = AdminDevActorBootstrap.Snapshot;
        if (seller is null || admin is null)
            return;

        var access = provider.GetRequiredService<Tooba.AccessControl.Application.IAccessControlDirectory>();
        await access.EnsureBootstrapAsync(
            admin.ActorUserId,
            [seller.ActorA.SellerPartyId],
            tenant.TenantId.Value,
            CancellationToken.None);
        await access.SyncUserCapabilityTuplesAsync(
            seller.ActorA.ActorUserId,
            new Tooba.AccessControl.Application.AccessOwnerScope(
                Tooba.AccessControl.Domain.AccessOwnerScopeKind.Seller,
                seller.ActorA.SellerPartyId,
                tenant.TenantId.Value),
            CancellationToken.None);

        await SupportDevelopmentSeed.ApplyAsync(
            provider,
            StorefrontCheckoutComposer.StorefrontGuestActorId,
            seller.ActorA.SellerPartyId,
            seller.ActorA.ActorUserId,
            admin.ActorUserId,
            CancellationToken.None);
    }
}
