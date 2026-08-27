using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.PlatformProbe.Infrastructure.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Promotion.Infrastructure.Persistence;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence;
using Tooba.Reviews.Infrastructure.Persistence;
using Tooba.ProductQnA.Infrastructure.Persistence;
using Tooba.BulkInquiry.Infrastructure.Persistence;
using Tooba.Host.Wishlist;
using Tooba.Host.AddressBook;
using Tooba.Host.CustomerProfile;
using Tooba.Wishlist.Infrastructure.Persistence;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.PageComposition.Infrastructure;
using Tooba.PageComposition.Infrastructure.Persistence;
using Tooba.Reviews.Infrastructure;

namespace Tooba.Host.Admin;

/// <summary>
/// مهاجرت Development و درج نمونه از مسیر دایرکتوری‌های ماژول، نه JSON جعلی UI.
/// داده فقط پس از خواندن مجدد HTTP به‌عنوان شواهد زنده پذیرفته می‌شود.
/// </summary>
internal static class ProductWorkspaceDevelopmentBootstrap
{
    internal const string SeedSlug = "workspace-live-shirt";

    /// <summary>
    /// schemaها را روی Tenant Development اعمال می‌کند و در صورت نبودن نمونه، Catalog/Offer/Price/Tax/Inventory را از دایرکتوری می‌نویسد.
    /// در Production صدا زده نمی‌شود. SQL بین‌ماژولی نوشته نمی‌شود.
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider services)
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
            "workspace-dev-seed"));

        await MigrateAsync(provider.GetRequiredService<CatalogDbContext>());
        await MigrateAsync(provider.GetRequiredService<OfferDbContext>());
        await MigrateAsync(provider.GetRequiredService<PricingDbContext>());
        await MigrateAsync(provider.GetRequiredService<InventoryDbContext>());
        await MigrateAsync(provider.GetRequiredService<TaxDbContext>());
        await MigrateAsync(provider.GetRequiredService<PartyDbContext>());
        await MigrateAsync(provider.GetRequiredService<IdentityDbContext>());
        await MigrateAsync(provider.GetRequiredService<CartDbContext>());
        await MigrateAsync(provider.GetRequiredService<OrderDbContext>());
        await MigrateAsync(provider.GetRequiredService<PaymentDbContext>());
        await MigrateAsync(provider.GetRequiredService<PromotionDbContext>());
        await MigrateAsync(provider.GetRequiredService<PlatformProbeDbContext>());
        await MigrateAsync(provider.GetRequiredService<ReviewsDbContext>());
        await MigrateAsync(provider.GetRequiredService<ProductQnADbContext>());
        await MigrateAsync(provider.GetRequiredService<BulkInquiryDbContext>());
        await MigrateAsync(provider.GetRequiredService<WishlistDbContext>());
        await MigrateAsync(provider.GetRequiredService<AddressBookDbContext>());
        await MigrateAsync(provider.GetRequiredService<CustomerProfileDbContext>());
        await MigrateAsync(provider.GetRequiredService<ContentDbContext>());
        await MigrateAsync(provider.GetRequiredService<PageCompositionDbContext>());

        var catalogDb = provider.GetRequiredService<CatalogDbContext>();
        var partyDb = provider.GetRequiredService<PartyDbContext>();
        if (await catalogDb.Products.AnyAsync(product => product.SlugSeam == SeedSlug))
        {
            await RefreshOperatorFacingCopyAsync(catalogDb, partyDb);
            await Seller.SellerDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
            await AdminDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
            await ReviewsDevelopmentSeed.ApplyAsync(provider);
            await WishlistDevelopmentSeed.ApplyAsync(provider);
            await AddressBookDevelopmentSeed.ApplyAsync(provider);
            await CustomerProfileDevelopmentSeed.ApplyAsync(provider);
            await ContentDevelopmentSeed.ApplyAsync(provider);
            await PageCompositionDevelopmentSeed.ApplyAsync(provider);
            return;
        }

        var catalog = provider.GetRequiredService<ICatalogDirectory>();
        var parties = provider.GetRequiredService<IPartyDirectory>();
        var offers = provider.GetRequiredService<IOfferDirectory>();
        var prices = provider.GetRequiredService<IPriceDirectory>();
        var inventory = provider.GetRequiredService<IInventoryDirectory>();
        var tax = provider.GetRequiredService<ITaxDirectory>();
        var cancellation = CancellationToken.None;

        var productNames = new Dictionary<string, string>
        {
            ["fa-IR"] = "پیراهن مردانه لینن",
            ["en-US"] = "Men's Linen Shirt",
        };
        var categoryNames = new Dictionary<string, string>
        {
            ["fa-IR"] = "پوشاک مردانه",
            ["en-US"] = "Men's apparel",
        };
        var brandNames = new Dictionary<string, string>
        {
            ["fa-IR"] = "آرمان",
            ["en-US"] = "Arman",
        };
        var category = await catalog.CreateCategoryAsync(null, categoryNames, cancellation);
        var brand = await catalog.CreateBrandAsync("tooba-live", brandNames, cancellation);
        var colorId = await catalog.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ", ["en-US"] = "Color" },
            cancellation);
        var black = await catalog.AddAttributeOptionAsync(
            colorId,
            "black",
            new Dictionary<string, string> { ["fa-IR"] = "سیاه", ["en-US"] = "Black" },
            cancellation);

        var product = await catalog.CreateProductAsync(CatalogProductKind.PhysicalGood, SeedSlug, brand.BrandId, productNames, cancellation);
        await catalog.AssignCategoryAsync(product.ProductId, category.CategoryId, cancellation);
        await catalog.AttachMediaReferenceAsync(product.ProductId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), cancellation);
        await catalog.PublishProductAsync(product.ProductId, cancellation);
        var variant = await catalog.CreateVariantAsync(
            product.ProductId,
            "LIVE-SHIRT-BLK",
            [(colorId, "ignored", black)],
            cancellation);

        var sellerA = await parties.CreateOrganizationAsync("فروشگاه آرمان", "Arman Store Legal", cancellation);
        var sellerB = await parties.CreateOrganizationAsync("دیجی‌استایل نمونه", "Digistyle Sample Legal", cancellation);
        var offerA = await offers.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Marketplace, "ARM-LN-01", cancellation);
        var offerB = await offers.CreateOfferAsync(variant.VariantId, sellerB.PartyId, SalesChannel.Marketplace, "DGS-LN-01", cancellation);
        await offers.ActivateAsync(offerA.OfferId, cancellation);
        await offers.ActivateAsync(offerB.OfferId, cancellation);

        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var priceA = await prices.CreatePriceAsync(offerA.OfferId, "IR", SalesChannel.Marketplace, 1850000, "IRR", start, null, cancellation);
        var priceB = await prices.CreatePriceAsync(offerB.OfferId, "IR", SalesChannel.Marketplace, 1790000, "IRR", start, null, cancellation);
        await prices.ActivateAsync(priceA.PriceId, cancellation);
        await prices.ActivateAsync(priceB.PriceId, cancellation);

        var taxCategory = await tax.CreateCategoryAsync("standard", "استاندارد", cancellation);
        await tax.AssignOfferCategoryAsync(offerA.OfferId, taxCategory.CategoryId, cancellation);
        await tax.AssignOfferCategoryAsync(offerB.OfferId, taxCategory.CategoryId, cancellation);
        var rule = await tax.CreateRuleAsync(
            "IR-NAT",
            "IR",
            taxCategory.CategoryId,
            TaxRuleKind.Percentage,
            0.09m,
            start,
            null,
            10,
            TaxOverridePolicy.Disabled,
            cancellation);
        await tax.ActivateRuleAsync(rule.RuleId, cancellation);

        var locThr = await inventory.CreateLocationAsync("WH-THR", "انبار مرکزی تهران", cancellation);
        var locIsf = await inventory.CreateLocationAsync("WH-ISF", "انبار اصفهان", cancellation);
        var locKsh = await inventory.CreateLocationAsync("WH-KSH", "انبار کاشان", cancellation);
        var stockA1 = await inventory.OpenPositionAsync(offerA.OfferId, locThr, cancellation);
        var stockA2 = await inventory.OpenPositionAsync(offerA.OfferId, locIsf, cancellation);
        var stockB1 = await inventory.OpenPositionAsync(offerB.OfferId, locKsh, cancellation);
        await inventory.AdjustAsync(stockA1, StockAdjustmentKind.Increase, 12, "seed-receipt", null, cancellation);
        await inventory.AdjustAsync(stockA2, StockAdjustmentKind.Increase, 7, "seed-receipt", null, cancellation);
        await inventory.AdjustAsync(stockB1, StockAdjustmentKind.Increase, 4, "seed-receipt", null, cancellation);
        await inventory.ReserveAsync(stockA1, 3, "workspace-live-hold", "workspace-live-hold", null, cancellation);

        await Seller.SellerDevActorBootstrap.EnsureAsync(provider, cancellation);
        await AdminDevActorBootstrap.EnsureAsync(provider, cancellation);
        await ReviewsDevelopmentSeed.ApplyAsync(provider, cancellation);
        await WishlistDevelopmentSeed.ApplyAsync(provider, cancellation);
        await AddressBookDevelopmentSeed.ApplyAsync(provider, cancellation);
        await CustomerProfileDevelopmentSeed.ApplyAsync(provider, cancellation);
        await ContentDevelopmentSeed.ApplyAsync(provider, cancellation);
        await PageCompositionDevelopmentSeed.ApplyAsync(provider, cancellation);
    }

    /// <summary>
    /// برچسب‌های نمایشی نمونهٔ زنده را برای اپراتور فارسی به‌روز می‌کند؛ schema را بازنویسی نمی‌کند.
    /// </summary>
    private static async Task RefreshOperatorFacingCopyAsync(CatalogDbContext catalogDb, PartyDbContext partyDb)
    {
        var product = await catalogDb.Products.AsNoTracking().SingleAsync(item => item.SlugSeam == SeedSlug);
        foreach (var text in catalogDb.LocalizedTexts.Where(item => item.FieldKey == "name"))
        {
            var persian = text.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            if (text.OwnerKind == CatalogLocalizedOwnerKind.Product
                && text.Value is "پیراهن Workspace زنده" or "Live Workspace Shirt")
            {
                text.Value = persian ? "پیراهن مردانه لینن" : "Men's Linen Shirt";
            }
            else if (text.OwnerKind == CatalogLocalizedOwnerKind.Category
                && text.Value is "پیراهن Workspace زنده" or "Live Workspace Shirt" or "پیراهن مردانه لینن" or "Men's Linen Shirt")
            {
                text.Value = persian ? "پوشاک مردانه" : "Men's apparel";
            }
            else if (text.OwnerKind == CatalogLocalizedOwnerKind.Brand
                && text.Value is "پیراهن Workspace زنده" or "Live Workspace Shirt" or "پیراهن مردانه لینن" or "Men's Linen Shirt")
            {
                text.Value = persian ? "آرمان" : "Arman";
            }
        }

        foreach (var party in partyDb.Parties)
        {
            if (party.DisplayName is "فروشنده الف" or "Seller A")
            {
                party.DisplayName = "فروشگاه آرمان";
            }
            else if (party.DisplayName is "فروشنده ب" or "Seller B")
            {
                party.DisplayName = "دیجی‌استایل نمونه";
            }
        }

        await catalogDb.SaveChangesAsync();
        await partyDb.SaveChangesAsync();
    }

    private static Task MigrateAsync(DbContext context) => context.Database.MigrateAsync();
}
