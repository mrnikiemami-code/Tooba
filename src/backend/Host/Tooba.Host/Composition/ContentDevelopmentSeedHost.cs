using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
using Tooba.Content.Infrastructure.Development;

namespace Tooba.Host.Composition;

/// <summary>
/// Thin Host composition seam for Content Development seed: binds store-alpha CommerceContext,
/// then delegates migrate/seed to Content.Infrastructure.
/// </summary>
internal static class ContentDevelopmentSeedHost
{
    /// <summary>Runs only when store-alpha is Active.</summary>
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

        await ContentDevelopmentSeedBootstrap.ApplyAsync(provider);
    }
}
