using Tooba.BuildingBlocks;
using Tooba.Catalog.Infrastructure.Development;

namespace Tooba.Host.Development;

/// <summary>اعمال دانه Landing دمو با زمینهٔ فروشگاه توسعه (Host commerce assigner).</summary>
internal static class LandingPageDevelopmentSeedHost
{
    /// <summary>دانه را روی Host توسعه اجرا می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
        {
            return;
        }

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
            "landing-dev-seed"));
        await LandingPageDevelopmentSeed.ApplyAsync(provider);
    }
}
