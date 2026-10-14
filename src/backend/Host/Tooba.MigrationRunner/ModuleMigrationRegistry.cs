using Microsoft.EntityFrameworkCore;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.BulkInquiry.Infrastructure.Persistence;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Infrastructure.Persistence;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.PageComposition.Infrastructure.Persistence;
using global::Tooba.Story.Infrastructure.Persistence;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.UserPreference.Infrastructure.Persistence;
using Tooba.OperatorProfile.Infrastructure.Persistence;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Settlement.Infrastructure.Persistence;
using Tooba.Notification.Infrastructure.Persistence;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.Support.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Wallet.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.ProductQnA.Infrastructure.Persistence;
using Tooba.Reviews.Infrastructure.Persistence;
using Tooba.Wishlist.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.MigrationRunner;

/// <summary>
/// فهرست ثابت DbContextهای ماژول به ترتیب اعمال مهاجرت تولید.
/// </summary>
internal static class ModuleMigrationRegistry
{
    /// <summary>
    /// توصیف یک ماژول قابل مهاجرت EF.
    /// </summary>
    internal sealed record ModuleMigrationDescriptor(
        string Module,
        string Schema,
        Func<string, DbContext> CreateContext);

    /// <summary>
    /// ترتیب ماژول‌ها با bootstrap توسعه هم‌تراز است.
    /// </summary>
    internal static IReadOnlyList<ModuleMigrationDescriptor> All { get; } =
    [
        Descriptor<CatalogDbContext>("Catalog", CatalogDbContext.Schema),
        new ModuleMigrationDescriptor(Tooba.Offer.Infrastructure.Adapters.OfferModuleMigration.Module, Tooba.Offer.Infrastructure.Adapters.OfferModuleMigration.Schema, Tooba.Offer.Infrastructure.Adapters.OfferModuleMigration.CreateContext),
        new ModuleMigrationDescriptor(Tooba.Pricing.Infrastructure.Adapters.PricingModuleMigration.Module, Tooba.Pricing.Infrastructure.Adapters.PricingModuleMigration.Schema, Tooba.Pricing.Infrastructure.Adapters.PricingModuleMigration.CreateContext),
        new ModuleMigrationDescriptor(Tooba.Inventory.Infrastructure.Adapters.InventoryModuleMigration.Module, Tooba.Inventory.Infrastructure.Adapters.InventoryModuleMigration.Schema, Tooba.Inventory.Infrastructure.Adapters.InventoryModuleMigration.CreateContext),
        new ModuleMigrationDescriptor(Tooba.Tax.Infrastructure.Adapters.TaxModuleMigration.Module, Tooba.Tax.Infrastructure.Adapters.TaxModuleMigration.Schema, Tooba.Tax.Infrastructure.Adapters.TaxModuleMigration.CreateContext),
        Descriptor<PartyDbContext>("Party", PartyDbContext.Schema),
        Descriptor<IdentityDbContext>("Identity", IdentityDbContext.Schema),
        Descriptor<CartDbContext>("Cart", CartDbContext.Schema),
        Descriptor<OrderDbContext>("Order", OrderDbContext.Schema),
        Descriptor<PaymentDbContext>("Payment", PaymentDbContext.Schema),
        Descriptor<FulfillmentDbContext>("Fulfillment", FulfillmentDbContext.Schema),
        Descriptor<ReturnsDbContext>("Returns", ReturnsDbContext.Schema),
        Descriptor<SettlementDbContext>("Settlement", SettlementDbContext.Schema),
        Descriptor<SupportDbContext>("Support", SupportDbContext.Schema),
        Descriptor<WalletDbContext>("Wallet", WalletDbContext.Schema),
        Descriptor<NotificationDbContext>("Notification", NotificationDbContext.Schema),
        Descriptor<AccessControlDbContext>("AccessControl", AccessControlDbContext.Schema),
        new ModuleMigrationDescriptor(Tooba.Promotion.Infrastructure.Adapters.PromotionModuleMigration.Module, Tooba.Promotion.Infrastructure.Adapters.PromotionModuleMigration.Schema, Tooba.Promotion.Infrastructure.Adapters.PromotionModuleMigration.CreateContext),
        Descriptor<ReviewsDbContext>("Reviews", ReviewsDbContext.Schema),
        Descriptor<ProductQnADbContext>("ProductQnA", ProductQnADbContext.Schema),
        Descriptor<BulkInquiryDbContext>("BulkInquiry", BulkInquiryDbContext.Schema),
        Descriptor<WishlistDbContext>("Wishlist", WishlistDbContext.Schema),
        Descriptor<AddressBookDbContext>("AddressBook", AddressBookDbContext.Schema),
        Descriptor<CustomerProfileDbContext>("CustomerProfile", CustomerProfileDbContext.Schema),
        Descriptor<UserPreferenceDbContext>("UserPreference", UserPreferenceDbContext.Schema),
        Descriptor<OperatorProfileDbContext>("OperatorProfile", OperatorProfileDbContext.Schema),
        Descriptor<LocalizationDbContext>("Localization", LocalizationDbContext.Schema),
        Descriptor<ContentDbContext>("Content", ContentDbContext.Schema),
        Descriptor<MediaDbContext>("Media", MediaDbContext.Schema),
        Descriptor<PageCompositionDbContext>("PageComposition", PageCompositionDbContext.Schema),
        Descriptor<StoryDbContext>("Story", StoryDbContext.Schema),
        // Geography is an independent reference-data module appended last: its schema has no foreign key
        // outside itself, so it neither reorders nor constrains any existing descriptor. Registering this
        // descriptor only makes the module visible to the explicit CLI (status/plan/apply); it executes
        // nothing by itself and is never reached by Host startup (see SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045).
        Descriptor<GeographyDbContext>("Geography", GeographyDbContext.Schema),
        // Shipping is appended last for the same reason as Geography: its own schema has no foreign key
        // outside itself, so it neither reorders nor constrains any existing descriptor. This descriptor
        // is metadata for the explicit CLI (status/plan/apply) only; it executes nothing by itself and is
        // never reached by Host startup, which registers no Shipping IModuleSchemaMigrator
        // (see SHIPPING-MVP-ACTIVATION-053).
        Descriptor<ShippingDbContext>("Shipping", ShippingDbContext.Schema),
    ];

    private static ModuleMigrationDescriptor Descriptor<TContext>(string module, string schema)
        where TContext : DbContext
    {
        return new ModuleMigrationDescriptor(
            module,
            schema,
            connectionString =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<TContext>();
                ToobaNpgsql.ConfigureModuleContext(
                    optionsBuilder,
                    connectionString,
                    schema,
                    typeof(TContext));
                return (DbContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
            });
    }
}
