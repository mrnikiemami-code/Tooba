using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.Catalog.Application.Development.CatalogDemo;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// Development composition seam owned by Catalog. The Host only registers the neutral
/// <c>AddModuleSchemaMigrationStep</c> seam; every Catalog Development prerequisite detail stays here.
/// </summary>
public static class CatalogDevelopmentSeed
{
    /// <summary>
    /// Mandatory Catalog Development steps that must run after the Catalog schema migration.
    /// </summary>
    public static class PostMigration
    {
        /// <summary>
        /// Runs the Catalog-owned Development attribute-schema prerequisite (LOCK-SF-402 sellable
        /// attribute workflow) idempotently. No-op outside Development and when the legacy bootstrap
        /// flag is disabled, matching the previous Host behavior.
        /// </summary>
        public static async Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken)
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            if (!environment.IsDevelopment())
            {
                return;
            }

            var options = provider.GetRequiredService<IOptions<CatalogDemoSeedOptions>>().Value;
            if (!options.RunLegacyBootstraps)
            {
                return;
            }

            await CatalogAttributeSchemaDevelopmentSeed.ApplyAsync(provider, cancellationToken);
        }
    }

    /// <summary>
    /// Runs the optional legacy Catalog demo bootstraps (Fashion/Industry template catalogs,
    /// landing pages, store menu) idempotently. No-op outside Development and when the legacy
    /// bootstrap flag is disabled.
    /// </summary>
    public static async Task EnsureLegacyBootstrapsAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var environment = provider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var options = provider.GetRequiredService<IOptions<CatalogDemoSeedOptions>>().Value;
        if (!options.RunLegacyBootstraps)
        {
            return;
        }

        await FashionTemplateCatalogSeed.ApplyAsync(provider, cancellationToken);
        await IndustryBatchATemplateCatalogSeed.ApplyAsync(provider, cancellationToken);
        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(provider, cancellationToken);
        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(provider, cancellationToken);
        await LandingPageDevelopmentSeed.ApplyAsync(provider, cancellationToken);
        await StoreMenuDevelopmentSeed.ApplyAsync(provider, cancellationToken);
    }
}
