using Microsoft.EntityFrameworkCore;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.BulkInquiry.Infrastructure.Persistence;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.PlatformProbe.Infrastructure.Persistence;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.ProductQnA.Infrastructure.Persistence;
using Tooba.Promotion.Infrastructure.Persistence;
using Tooba.Reviews.Infrastructure.Persistence;
using Tooba.Tax.Infrastructure.Persistence;
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
        Descriptor<OfferDbContext>("Offer", OfferDbContext.Schema),
        Descriptor<PricingDbContext>("Pricing", PricingDbContext.Schema),
        Descriptor<InventoryDbContext>("Inventory", InventoryDbContext.Schema),
        Descriptor<TaxDbContext>("Tax", TaxDbContext.Schema),
        Descriptor<PartyDbContext>("Party", PartyDbContext.Schema),
        Descriptor<IdentityDbContext>("Identity", IdentityDbContext.Schema),
        Descriptor<CartDbContext>("Cart", CartDbContext.Schema),
        Descriptor<OrderDbContext>("Order", OrderDbContext.Schema),
        Descriptor<PaymentDbContext>("Payment", PaymentDbContext.Schema),
        Descriptor<PromotionDbContext>("Promotion", PromotionDbContext.Schema),
        Descriptor<PlatformProbeDbContext>("PlatformProbe", PlatformProbeDbContext.Schema),
        Descriptor<ReviewsDbContext>("Reviews", ReviewsDbContext.Schema),
        Descriptor<ProductQnADbContext>("ProductQnA", ProductQnADbContext.Schema),
        Descriptor<BulkInquiryDbContext>("BulkInquiry", BulkInquiryDbContext.Schema),
        Descriptor<WishlistDbContext>("Wishlist", WishlistDbContext.Schema),
        Descriptor<AddressBookDbContext>("AddressBook", AddressBookDbContext.Schema),
        Descriptor<CustomerProfileDbContext>("CustomerProfile", CustomerProfileDbContext.Schema),
        Descriptor<ContentDbContext>("Content", ContentDbContext.Schema),
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
