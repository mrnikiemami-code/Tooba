using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Host.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Models;
using Tooba.AddressBook.Contracts.Customer;
using Tooba.Cart.Application.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class IndustryBatchCTemplateCatalogT022R12CTests
{
    public static TheoryData<string, Guid> PackKeys => new()
    {
        { ShoesTemplateCatalogIds.Key, ShoesTemplateCatalogIds.TemplateId },
        { PlantsTemplateCatalogIds.Key, PlantsTemplateCatalogIds.TemplateId },
        { BeautyTemplateCatalogIds.Key, BeautyTemplateCatalogIds.TemplateId },
    };

    [Fact]
    public async Task Batch_C_keys_are_unique_and_seed_is_idempotent()
    {
        await using var catalog = CreateCatalog();
        var provider = new SimpleServiceProvider(catalog);

        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(provider);
        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(provider);

        Assert.Equal(3, await catalog.StoreTemplates.CountAsync());
        var keys = await catalog.StoreTemplates.Select(x => x.Key).OrderBy(x => x).ToListAsync();
        Assert.Equal(
            new[]
            {
                BeautyTemplateCatalogIds.Key,
                PlantsTemplateCatalogIds.Key,
                ShoesTemplateCatalogIds.Key,
            }.OrderBy(x => x),
            keys);
        Assert.Equal(3, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [MemberData(nameof(PackKeys))]
    public async Task Each_pack_has_exact_tree_and_product_counts(string key, Guid templateId)
    {
        await using var catalog = CreateCatalog();
        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

        Assert.Equal(15, await catalog.TemplateProducts.CountAsync(x => x.TemplateId == templateId));
        Assert.Equal(6, await catalog.TemplateBrands.CountAsync(x => x.TemplateId == templateId));

        var roots = await catalog.TemplateCategories
            .Where(x => x.TemplateId == templateId && x.ParentCategoryId == null)
            .ToListAsync();
        Assert.Equal(8, roots.Count);

        foreach (var root in roots)
        {
            var mids = await catalog.TemplateCategories
                .Where(x => x.ParentCategoryId == root.CategoryId)
                .ToListAsync();
            Assert.Equal(2, mids.Count);
            foreach (var mid in mids)
            {
                var leaves = await catalog.TemplateCategories.CountAsync(x => x.ParentCategoryId == mid.CategoryId);
                Assert.Equal(2, leaves);
            }
        }

        Assert.Equal(
            15,
            await catalog.TemplateLocalizedTexts.CountAsync(x =>
                x.OwnerKind == TemplateLocalizedOwnerKind.Product
                && x.Locale == IndustryBatchCTemplateCatalogSeed.LocaleFa
                && catalog.TemplateProducts.Any(p => p.ProductId == x.OwnerId && p.TemplateId == templateId)));
        Assert.True(await catalog.TemplateStoreLandingPageSections.AnyAsync(x =>
            x.SectionType == "BannerShowcase"
            && catalog.TemplateStoreLandingPages.Any(p => p.PageId == x.PageId && p.TemplateKey == key)));
        Assert.Equal(key, (await catalog.StoreTemplates.SingleAsync(x => x.TemplateId == templateId)).Key);
    }

    [Theory]
    [MemberData(nameof(PackKeys))]
    public async Task Preview_is_pure_and_media_is_isolated(string key, Guid templateId)
    {
        await using var catalog = CreateCatalog();
        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

        catalog.Products.Add(CatalogProduct.Create(CatalogProductKind.PhysicalGood, "ops-only", DateTimeOffset.UtcNow));
        catalog.Categories.Add(CatalogCategory.Create(null, DateTimeOffset.UtcNow));
        catalog.Brands.Add(CatalogBrand.Create("ops-brand", DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();

        var query = new IndustryTemplatePreviewQuery(catalog);
        var preview = await query.GetSampleAsync(key);
        Assert.NotNull(preview);
        Assert.Equal(15, preview!.Products.Count);
        Assert.Equal(8, preview.Purity.TemplateTopLevelCategoryCount);
        Assert.Equal(0, preview.Purity.OperationalProductIdHits);
        Assert.Equal(0, preview.Purity.OperationalCategoryIdHits);
        Assert.Equal(0, preview.Purity.OperationalBrandIdHits);
        Assert.True(preview.Purity.IsPure);
        Assert.Equal(IndustryBatchCTemplateCatalogSeed.OriginFor(key), preview.Origin);
        Assert.Equal(templateId.ToString("D"), preview.TemplateId);
        var folder = key switch
        {
            "shoes" => "/images/template-shoes/",
            "plants" => "/images/template-plants/",
            _ => "/images/template-beauty/",
        };
        Assert.All(preview.Categories.Where(c => c.ImageUrl is not null), c =>
        {
            Assert.StartsWith(folder, c.ImageUrl!, StringComparison.Ordinal);
            Assert.DoesNotContain("/images/fashion-template/", c.ImageUrl!, StringComparison.Ordinal);
            Assert.DoesNotContain("/images/template-tile-ceramic/", c.ImageUrl!, StringComparison.Ordinal);
        });
        Assert.Contains(preview.Products, p => p.MediaUrl != null && p.MediaUrl.StartsWith(folder, StringComparison.Ordinal));
        Assert.DoesNotContain(preview.Products, p => p.MediaUrl != null && p.MediaUrl.Contains("/images/fashion-template/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Structural_parity_is_seeded_once_per_pack()
    {
        await using var catalog = CreateCatalog();
        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));
        await IndustryBatchCTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

        Assert.Equal(2, await catalog.TemplateAttributeDefinitions.CountAsync(x => x.TemplateId == ShoesTemplateCatalogIds.TemplateId));
        Assert.Equal(2, await catalog.TemplateAttributeDefinitions.CountAsync(x => x.TemplateId == PlantsTemplateCatalogIds.TemplateId));
        Assert.Equal(2, await catalog.TemplateAttributeDefinitions.CountAsync(x => x.TemplateId == BeautyTemplateCatalogIds.TemplateId));
        Assert.Equal(15, await catalog.TemplateVariants.CountAsync(x =>
            catalog.TemplateProducts.Any(p => p.ProductId == x.ProductId && p.TemplateId == ShoesTemplateCatalogIds.TemplateId)));
    }

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase("batch-c-t022-r12c-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private sealed class SimpleServiceProvider(CatalogDbContext catalog) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(CatalogDbContext) ? catalog : null;
    }
}

