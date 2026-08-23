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

        var catalogDb = provider.GetRequiredService<CatalogDbContext>();
        if (await catalogDb.Products.AnyAsync(product => product.SlugSeam == SeedSlug))
        {
            return;
        }

        var catalog = provider.GetRequiredService<ICatalogDirectory>();
        var parties = provider.GetRequiredService<IPartyDirectory>();
        var offers = provider.GetRequiredService<IOfferDirectory>();
        var prices = provider.GetRequiredService<IPriceDirectory>();
        var inventory = provider.GetRequiredService<IInventoryDirectory>();
        var tax = provider.GetRequiredService<ITaxDirectory>();
        var cancellation = CancellationToken.None;

        var names = new Dictionary<string, string>
        {
            ["fa-IR"] = "پیراهن Workspace زنده",
            ["en-US"] = "Live Workspace Shirt",
        };
        var category = await catalog.CreateCategoryAsync(null, names, cancellation);
        var brand = await catalog.CreateBrandAsync("tooba-live", names, cancellation);
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

        var product = await catalog.CreateProductAsync(CatalogProductKind.PhysicalGood, SeedSlug, brand.BrandId, names, cancellation);
        await catalog.AssignCategoryAsync(product.ProductId, category.CategoryId, cancellation);
        await catalog.AttachMediaReferenceAsync(product.ProductId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), cancellation);
        await catalog.PublishProductAsync(product.ProductId, cancellation);
        var variant = await catalog.CreateVariantAsync(
            product.ProductId,
            "LIVE-SHIRT-BLK",
            [(colorId, "ignored", black)],
            cancellation);

        var sellerA = await parties.CreateOrganizationAsync("فروشنده الف", "Seller A Legal", cancellation);
        var sellerB = await parties.CreateOrganizationAsync("فروشنده ب", "Seller B Legal", cancellation);
        var offerA = await offers.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Marketplace, "LIVE-A", cancellation);
        var offerB = await offers.CreateOfferAsync(variant.VariantId, sellerB.PartyId, SalesChannel.Marketplace, "LIVE-B", cancellation);
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

        var locThr = await inventory.CreateLocationAsync("WH-THR", "انبار تهران", cancellation);
        var locIsf = await inventory.CreateLocationAsync("WH-ISF", "انبار اصفهان", cancellation);
        var locKsh = await inventory.CreateLocationAsync("WH-KSH", "انبار فروشنده ب", cancellation);
        var stockA1 = await inventory.OpenPositionAsync(offerA.OfferId, locThr, cancellation);
        var stockA2 = await inventory.OpenPositionAsync(offerA.OfferId, locIsf, cancellation);
        var stockB1 = await inventory.OpenPositionAsync(offerB.OfferId, locKsh, cancellation);
        await inventory.AdjustAsync(stockA1, StockAdjustmentKind.Increase, 12, "seed-receipt", null, cancellation);
        await inventory.AdjustAsync(stockA2, StockAdjustmentKind.Increase, 7, "seed-receipt", null, cancellation);
        await inventory.AdjustAsync(stockB1, StockAdjustmentKind.Increase, 4, "seed-receipt", null, cancellation);
        await inventory.ReserveAsync(stockA1, 3, "workspace-live-hold", "workspace-live-hold", null, cancellation);
    }

    private static Task MigrateAsync(DbContext context) => context.Database.MigrateAsync();
}
