using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Catalog.Application.Development;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.PlatformProbe.Infrastructure.Persistence;
using Tooba.Pricing.Contracts;
using Tooba.Promotion.Application.Merchandising;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Promotion.Infrastructure.Development;
using Tooba.Reviews.Infrastructure;
using Tooba.Tax.Contracts;
using Tooba.Host.Wishlist;
using Tooba.AddressBook.Infrastructure.Adapters;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.CustomerProfile.Infrastructure.Development;
using Tooba.Host.Admin.Development;
using Tooba.Host.Seller;
using Tooba.Host.Settings;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Development;
using Tooba.PageComposition.Infrastructure;
using global::Tooba.Story.Infrastructure;
using Tooba.Wishlist.Infrastructure.Persistence;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.UserPreference.Infrastructure.Persistence;
using Tooba.OperatorProfile.Infrastructure.Persistence;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.PageComposition.Infrastructure.Persistence;
using global::Tooba.Story.Infrastructure.Persistence;
using Tooba.Notification.Infrastructure.Persistence;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.Reviews.Infrastructure.Persistence;
using Tooba.ProductQnA.Infrastructure.Persistence;
using Tooba.BulkInquiry.Infrastructure.Persistence;

namespace Tooba.Host.Development;

/// <summary>
/// مهاجرت Development و ترتیب اجرای دانه‌های Development.
/// Host هیچ داده یا policy تجاری ندارد: دانهٔ Catalog/Offer/Price/Tax/Inventory از
/// <see cref="IWorkspaceDemoSeed"/> ماژول Catalog می‌آید و بقیهٔ دانه‌ها مالکیت ماژول خود را دارند.
/// </summary>
internal static class ProductWorkspaceDevelopmentBootstrap
{
    /// <summary>
    /// فقط schema Tenant Development را اعمال می‌کند و دانهٔ Catalog را نمی‌نویسد.
    /// وقتی RunLegacyBootstraps=false است باید صدا زده شود تا مهاجرت‌های رزرو اعمال شوند.
    /// </summary>
    public static Task MigrateSchemaOnlyAsync(IServiceProvider services)
        => ApplyCoreAsync(services, seedCatalog: false);

    /// <summary>
    /// schemaها را روی Tenant Development اعمال می‌کند و در صورت نبودن نمونه،
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
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
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

        await MigrateAsync(provider.GetRequiredService<CatalogDbContext>());
        await provider.GetRequiredService<IOfferSchemaMigrator>().MigrateAsync();
        await provider.GetRequiredService<IPricingSchemaMigrator>().MigrateAsync();
        await provider.GetRequiredService<IInventorySchemaMigrator>().MigrateAsync();
        await provider.GetRequiredService<ITaxSchemaMigrator>().MigrateAsync();
        await MigrateAsync(provider.GetRequiredService<PartyDbContext>());
        await MigrateAsync(provider.GetRequiredService<IdentityDbContext>());
        await MigrateAsync(provider.GetRequiredService<CartDbContext>());
        await MigrateAsync(provider.GetRequiredService<OrderDbContext>());
        await MigrateAsync(provider.GetRequiredService<PaymentDbContext>());
        await MigrateAsync(provider.GetRequiredService<FulfillmentDbContext>());
        await provider.GetRequiredService<IPromotionSchemaMigrator>().MigrateAsync();
        await provider.GetRequiredService<IMerchandisingCampaignDirectory>()
            .EnsureAmazingTypeSeededAsync(CancellationToken.None);
        // Development AMAZING campaigns must seed even when legacy Catalog bootstraps are off
        // (RunLegacyBootstraps=false → MigrateSchemaOnly), so Storefront/Builder PromotionCampaign
        // sources have real runtime data (LOCK-SF-402).
        await MerchandisingCampaignDevelopmentSeed.EnsureAsync(provider, CancellationToken.None);
        await MigrateAsync(provider.GetRequiredService<PlatformProbeDbContext>());
        await MigrateAsync(provider.GetRequiredService<ReviewsDbContext>());
        await MigrateAsync(provider.GetRequiredService<ProductQnADbContext>());
        await MigrateAsync(provider.GetRequiredService<BulkInquiryDbContext>());
        await MigrateAsync(provider.GetRequiredService<WishlistDbContext>());
        await MigrateAsync(provider.GetRequiredService<AddressBookDbContext>());
        await MigrateAsync(provider.GetRequiredService<CustomerProfileDbContext>());
        await MigrateAsync(provider.GetRequiredService<UserPreferenceDbContext>());
        await MigrateAsync(provider.GetRequiredService<OperatorProfileDbContext>());
        await MigrateAsync(provider.GetRequiredService<ContentDbContext>());
        await MigrateAsync(provider.GetRequiredService<MediaDbContext>());
        await MigrateAsync(provider.GetRequiredService<PageCompositionDbContext>());
        await MigrateAsync(provider.GetRequiredService<StoryDbContext>());
        await MigrateAsync(provider.GetRequiredService<NotificationDbContext>());
        await MigrateAsync(provider.GetRequiredService<AccessControlDbContext>());
        await MigrateAsync(provider.GetRequiredService<Tooba.Support.Infrastructure.Persistence.SupportDbContext>());

        if (!seedCatalog)
        {
            return;
        }

        var workspaceDemo = provider.GetRequiredService<IWorkspaceDemoSeed>();
        if (await workspaceDemo.IsLiveProductSeededAsync(CancellationToken.None))
        {
            await workspaceDemo.RefreshExistingCopyAsync(CancellationToken.None);
            await workspaceDemo.EnsureAdminR3PreviewAsync(CancellationToken.None);
            await SellerDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
            await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
            await ReviewsDevelopmentSeed.ApplyAsync(provider);
            await WishlistDevelopmentSeed.ApplyAsync(provider);
            await AddressBookDevelopmentSeed.ApplyAsync(provider);
            await CustomerProfileDevelopmentSeed.ApplyAsync(provider);
            await SettingsFoundationDevelopmentSeed.ApplyAsync(provider);
            await ContentDevelopmentSeed.ApplyAsync(provider);
            await PageCompositionDevelopmentSeed.ApplyAsync(provider);
            await StoryDevelopmentSeed.ApplyAsync(provider);
            await Tooba.Catalog.Infrastructure.Development.LandingPageDevelopmentSeed.ApplyAsync(provider);
            await Tooba.Catalog.Infrastructure.Development.StoreMenuDevelopmentSeed.ApplyAsync(provider);
            await MerchandisingCampaignDevelopmentSeed.EnsureAsync(provider, CancellationToken.None);
            return;
        }

        await workspaceDemo.SeedNewProductAsync(CancellationToken.None);

        await SellerDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        await ReviewsDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await WishlistDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await AddressBookDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await CustomerProfileDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await SettingsFoundationDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await ContentDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await PageCompositionDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await StoryDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await Tooba.Catalog.Infrastructure.Development.LandingPageDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await Tooba.Catalog.Infrastructure.Development.StoreMenuDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await workspaceDemo.EnsureAdminR3PreviewAsync(CancellationToken.None);
        await MerchandisingCampaignDevelopmentSeed.EnsureAsync(provider, CancellationToken.None);
    }

    private static Task MigrateAsync(DbContext context) => context.Database.MigrateAsync();
}
