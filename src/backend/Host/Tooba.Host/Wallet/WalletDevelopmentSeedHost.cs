using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Wallet.Infrastructure;
using Tooba.Wallet.Infrastructure.Persistence;

namespace Tooba.Host.Wallet;

/// <summary>اعمال دانهٔ توسعه Wallet روی scope با CommerceContext.</summary>
internal static class WalletDevelopmentSeedHost
{
    /// <summary>دانه را فقط در Development و با Actor آماده اجرا می‌کند.</summary>
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

        var db = provider.GetRequiredService<WalletDbContext>();
        await db.Database.MigrateAsync();

        await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        var admin = AdminDevActorBootstrap.Snapshot;
        if (admin is null)
            return;

        // همان Actor مشتری demo که Support استفاده می‌کند.
        var customerActorUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000009");

        await WalletDevelopmentSeed.ApplyAsync(
            provider,
            customerActorUserId,
            admin.ActorUserId,
            CancellationToken.None);
    }
}
