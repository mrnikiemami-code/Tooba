using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001 — the Host foreign-DbContext
/// migration orchestration is replaced by the neutral <c>IModuleSchemaMigrator</c> seam owned by
/// <c>Tooba.Persistence</c>; every module registers its own migrator from its own composition root.
/// </summary>
public sealed class HostDevelopmentMigrationSeamGuardTests
{
    private static readonly string[] ModuleCompositionRoots =
    [
        "Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs",
        "Offer/Tooba.Offer.Infrastructure/DependencyInjection/OfferModule.cs",
        "Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs",
        "Inventory/Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs",
        "Tax/Tooba.Tax.Infrastructure/DependencyInjection/TaxModule.cs",
        "Party/Tooba.Party.Infrastructure/PartyModule.cs",
        "Identity/Tooba.Identity.Infrastructure/IdentityModule.cs",
        "Cart/Tooba.Cart.Infrastructure/DependencyInjection/CartModule.cs",
        "Order/Tooba.Order.Infrastructure/OrderModule.cs",
        "Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs",
        "Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs",
        "Promotion/Tooba.Promotion.Infrastructure/DependencyInjection/PromotionModule.cs",
        "Reviews/Tooba.Reviews.Infrastructure/ReviewsModule.cs",
        "ProductQnA/Tooba.ProductQnA.Infrastructure/ProductQnAModule.cs",
        "BulkInquiry/Tooba.BulkInquiry.Infrastructure/BulkInquiryModule.cs",
        "Wishlist/Tooba.Wishlist.Infrastructure/WishlistModule.cs",
        "AddressBook/Tooba.AddressBook.Infrastructure/DependencyInjection/AddressBookModule.cs",
        "CustomerProfile/Tooba.CustomerProfile.Infrastructure/DependencyInjection/CustomerProfileModule.cs",
        "UserPreference/Tooba.UserPreference.Infrastructure/UserPreferenceModule.cs",
        "OperatorProfile/Tooba.OperatorProfile.Infrastructure/OperatorProfileModule.cs",
        "Localization/Tooba.Localization.Infrastructure/LocalizationModule.cs",
        "Content/Tooba.Content.Infrastructure/ContentModule.cs",
        "Media/Tooba.Media.Infrastructure/MediaModule.cs",
        "PageComposition/Tooba.PageComposition.Infrastructure/PageCompositionModule.cs",
        "Story/Tooba.Story.Infrastructure/DependencyInjection/StoryModule.cs",
        "Notification/Tooba.Notification.Infrastructure/DependencyInjection/NotificationModule.cs",
        "AccessControl/Tooba.AccessControl.Infrastructure/AccessControlModule.cs",
        "Support/Tooba.Support.Infrastructure/DependencyInjection/SupportModule.cs",
    ];

    [Fact]
    public void Neutral_seam_lives_in_Tooba_Persistence_with_stable_identity_and_order()
    {
        var seam = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs"));
        Assert.Contains("public interface IModuleSchemaMigrator", seam, StringComparison.Ordinal);
        Assert.Contains("string Module { get; }", seam, StringComparison.Ordinal);
        Assert.Contains("int Order { get; }", seam, StringComparison.Ordinal);
        Assert.Contains("Task MigrateAsync(CancellationToken", seam, StringComparison.Ordinal);
        Assert.Contains("public sealed class EfModuleSchemaMigrator<TContext>", seam, StringComparison.Ordinal);
        Assert.Contains("public static class ModuleSchemaMigrationOrder", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("SupportDbContext", seam, StringComparison.Ordinal);
    }

    [Fact]
    public void All_twenty_eight_active_modules_register_their_own_schema_migrator()
    {
        var modules = Path.Combine(FindRepoRoot(), "src/backend/Modules");
        var registrations = 0;
        foreach (var relative in ModuleCompositionRoots)
        {
            var path = Path.Combine(modules, relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), relative);
            var text = File.ReadAllText(path);
            if (Regex.IsMatch(text, @"AddModuleSchemaMigrator(<|\(|\s)", RegexOptions.CultureInvariant))
            {
                registrations++;
            }
        }

        Assert.Equal(28, registrations);
        Assert.DoesNotContain(
            ModuleCompositionRoots,
            relative => relative.Contains("PlatformProbe", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductWorkspace_migration_slice_has_no_foreign_DbContext_or_persistence_namespace()
    {
        var folder = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Development");
        // The Wave 2 slice is the neutral migrator seam plus the tenant context seam. The pre-existing
        // MarketplaceDevelopmentBootstrap.cs marketplace composition seam is out of scope for this task.
        var joined = File.ReadAllText(Path.Combine(folder, "DevelopmentSchemaMigrator.cs"))
            + File.ReadAllText(Path.Combine(folder, "DevelopmentTenantCommerceContext.cs"));
        foreach (var forbidden in new[]
                 {
                     "CatalogDbContext", "PartyDbContext", "IdentityDbContext", "CartDbContext",
                     "OrderDbContext", "PaymentDbContext", "FulfillmentDbContext",
                     "PlatformProbeDbContext", "ReviewsDbContext", "ProductQnADbContext",
                     "BulkInquiryDbContext", "WishlistDbContext", "AddressBookDbContext",
                     "CustomerProfileDbContext", "UserPreferenceDbContext", "OperatorProfileDbContext",
                     "ContentDbContext", "MediaDbContext", "PageCompositionDbContext", "StoryDbContext",
                     "NotificationDbContext", "AccessControlDbContext", "SupportDbContext",
                     "IOfferSchemaMigrator", "IPricingSchemaMigrator", "IInventorySchemaMigrator",
                     "ITaxSchemaMigrator", "IPromotionSchemaMigrator",
                     "Infrastructure.Persistence",
                 })
        {
            Assert.DoesNotContain(forbidden, joined, StringComparison.Ordinal);
        }

        // Host/Development still contains exactly the five classified files.
        Assert.Equal(5, Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly).Length);
        Assert.False(File.Exists(Path.Combine(folder, "ProductWorkspaceDevelopmentBootstrap.cs")));
    }

    [Fact]
    public void Development_schema_migrator_is_neutral_composition_only()
    {
        var text = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs"));
        Assert.Contains("IModuleSchemaMigrator", text, StringComparison.Ordinal);
        Assert.Contains("DevelopmentTenantCommerceContext.DevelopmentTenantId", text, StringComparison.Ordinal);
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("ProductWorkspaceDevelopmentBootstrap", program, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_records_wave2_and_evidence_exists()
    {
        var sot = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostDevelopmentProductWorkspaceMigrationSeam001\"", sot, StringComparison.Ordinal);
        Assert.Contains("MIGRATION_SEAM_001", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001")));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
