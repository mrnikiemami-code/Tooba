using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Reviews.Infrastructure;
using Tooba.AddressBook.Infrastructure.Adapters;
using Tooba.Catalog.Application.Development;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.CustomerProfile.Infrastructure.Development;
using Tooba.AccessControl.Application.Development.Seller;
using Tooba.Host.Admin.Development;
using Tooba.Host.Settings;
using Tooba.Content.Infrastructure.Development;
using Tooba.PageComposition.Infrastructure;
using global::Tooba.Story.Infrastructure;
using Tooba.Wishlist.Infrastructure.Development;

namespace Tooba.Host.Development;

/// <summary>
/// ترکیب میزبان برای مهاجرت Development و ترتیب اجرای دانه‌های Development.
/// Host هیچ DbContext یا نوع داخلی ماژول را برای مهاجرت نمی‌شناسد: فقط لنگر خنثی
/// <see cref="IModuleSchemaMigrator"/> را از ظرف resolve و به ترتیب صریح اجرا می‌کند.
/// این seam هیچ policy یا دادهٔ تجاری ندارد؛ دانهٔ Catalog/Offer/Price/Tax/Inventory از
/// <see cref="IWorkspaceDemoSeed"/> ماژول Catalog و بقیهٔ دانه‌ها از مالکیت ماژول خود می‌آیند.
/// </summary>
internal static class DevelopmentSchemaMigrator
{
    /// <summary>
    /// فقط schema فروشگاه Development را مهاجرت می‌کند و دانهٔ Catalog را نمی‌نویسد.
    /// وقتی RunLegacyBootstraps=false است صدا زده می‌شود تا مهاجرت‌های رزرو اعمال شوند.
    /// </summary>
    public static Task MigrateSchemaOnlyAsync(IServiceProvider services)
        => ApplyCoreAsync(services, seedCatalog: false);

    /// <summary>
    /// schemaها را روی فروشگاه Development اعمال می‌کند و در صورت نبودن نمونه،
    /// دانهٔ Catalog و بقیهٔ دانه‌های ماژول‌محور را با همان ترتیب قبلی اجرا می‌کند.
    /// در Production صدا زده نمی‌شود. SQL بین‌ماژولی نوشته نمی‌شود.
    /// </summary>
    public static Task ApplyAsync(IServiceProvider services)
        => ApplyCoreAsync(services, seedCatalog: true);

    private static async Task ApplyCoreAsync(IServiceProvider services, bool seedCatalog)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue(DevelopmentTenantCommerceContext.DevelopmentTenantId, out var tenant)
            || tenant.Status != TenantStatus.Active)
        {
            throw new InvalidOperationException("Development seed requires Active tenant store-alpha.");
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
            seedCatalog ? "workspace-dev-seed" : "workspace-dev-schema"));

        await MigrateOrderedAsync(provider, CancellationToken.None);

        if (!seedCatalog)
        {
            return;
        }

        var workspaceDemo = provider.GetRequiredService<IWorkspaceDemoSeed>();
        var sellerDevContexts = provider.GetRequiredService<ISellerDevContextStore>();
        if (await workspaceDemo.IsLiveProductSeededAsync(CancellationToken.None))
        {
            await workspaceDemo.RefreshExistingCopyAsync(CancellationToken.None);
            await workspaceDemo.EnsureAdminR3PreviewAsync(CancellationToken.None);
            await sellerDevContexts.EnsureAsync(CancellationToken.None);
            await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
            await ReviewsDevelopmentSeed.ApplyAsync(provider);
            await WishlistDevelopmentSeed.ApplyAsync(provider);
            await AddressBookDevelopmentSeed.ApplyAsync(provider);
            await CustomerProfileDevelopmentSeed.ApplyAsync(provider);
            await SettingsFoundationDevelopmentSeed.ApplyAsync(provider);
            await ContentDevelopmentSeed.ApplyAsync(provider);
            await PageCompositionDevelopmentSeed.ApplyAsync(provider);
            await StoryDevelopmentSeed.ApplyAsync(provider);
            await LandingPageDevelopmentSeed.ApplyAsync(provider);
            await StoreMenuDevelopmentSeed.ApplyAsync(provider);
            return;
        }

        await workspaceDemo.SeedNewProductAsync(CancellationToken.None);

        await sellerDevContexts.EnsureAsync(CancellationToken.None);
        await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        await ReviewsDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await WishlistDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await AddressBookDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await CustomerProfileDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await SettingsFoundationDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await ContentDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await PageCompositionDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await StoryDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await LandingPageDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await StoreMenuDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await workspaceDemo.EnsureAdminR3PreviewAsync(CancellationToken.None);
    }

    /// <summary>
    /// لنگر خنثی مهاجرت را به ترتیب صریح پایدار اجرا می‌کند، سپس گام‌های ماژول‌محور پس از
    /// مهاجرتِ همان ماژول را اجرا می‌کند. ترتیب تکراری/نامعتبر fail-fast رد می‌شود.
    /// </summary>
    private static async Task MigrateOrderedAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var migrators = provider.GetServices<IModuleSchemaMigrator>()
            .OrderBy(x => x.Order)
            .ToArray();

        var duplicateOrder = migrators.GroupBy(x => x.Order).FirstOrDefault(g => g.Count() > 1);
        if (duplicateOrder is not null)
        {
            throw new InvalidOperationException(
                "Duplicate Development schema migration order: " + duplicateOrder.Key);
        }

        var steps = provider.GetServices<IModuleSchemaMigrationStep>()
            .OrderBy(x => x.AfterOrder)
            .ThenBy(x => x.Module, StringComparer.Ordinal)
            .ToArray();

        foreach (var migrator in migrators)
        {
            await migrator.MigrateAsync(cancellationToken);

            foreach (var step in steps.Where(x => x.AfterOrder == migrator.Order))
            {
                await step.RunAsync(provider, cancellationToken);
            }
        }
    }
}
