using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Infrastructure.Persistence;
using Tooba.Media.Infrastructure.Persistence;

namespace Tooba.Host.Content;

/// <summary>
/// دانهٔ توسعه Content وقتی Catalog legacy خاموش است: scope + CommerceContext + migrate، نه resolve از root.
/// </summary>
internal static class ContentDevelopmentSeedHost
{
    /// <summary>اعمال مهاجرت Localization/Content/Media و دانهٔ idempotent مقالات دمو روی tenant Development.</summary>
    public static async Task ApplyAsync(IServiceProvider root)
    {
        await using var scope = root.CreateAsyncScope();
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
            "content-dev-seed"));

        await provider.GetRequiredService<LocalizationDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<ContentDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<MediaDbContext>().Database.MigrateAsync();
        await ContentDevelopmentSeed.ApplyAsync(provider);
    }
}
