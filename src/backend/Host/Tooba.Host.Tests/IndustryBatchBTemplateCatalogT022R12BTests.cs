using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Host.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Models;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.AddressBook.Contracts.Ports;
using Tooba.Cart.Application.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class IndustryBatchBTemplateCatalogT022R12BTests
{
    public static TheoryData<string, Guid> PackKeys => new()
    {
        { TileCeramicTemplateCatalogIds.Key, TileCeramicTemplateCatalogIds.TemplateId },
        { InteriorDecorTemplateCatalogIds.Key, InteriorDecorTemplateCatalogIds.TemplateId },
        { HomeAppliancesTemplateCatalogIds.Key, HomeAppliancesTemplateCatalogIds.TemplateId },
    };

    [Fact]
    public async Task Batch_B_keys_are_unique_and_seed_is_idempotent()
    {
        await using var catalog = CreateCatalog();
        var provider = new SimpleServiceProvider(catalog);

        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(provider);
        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(provider);

        Assert.Equal(3, await catalog.StoreTemplates.CountAsync());
        var keys = await catalog.StoreTemplates.Select(x => x.Key).OrderBy(x => x).ToListAsync();
        Assert.Equal(
            new[]
            {
                HomeAppliancesTemplateCatalogIds.Key,
                InteriorDecorTemplateCatalogIds.Key,
                TileCeramicTemplateCatalogIds.Key,
            }.OrderBy(x => x),
            keys);
        Assert.Equal(3, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [MemberData(nameof(PackKeys))]
    public async Task Each_pack_has_exact_tree_and_product_counts(string key, Guid templateId)
    {
        await using var catalog = CreateCatalog();
        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

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
                && x.Locale == IndustryBatchBTemplateCatalogSeed.LocaleFa
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
        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

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
        Assert.Equal(IndustryBatchBTemplateCatalogSeed.OriginFor(key), preview.Origin);
        Assert.Equal(templateId.ToString("D"), preview.TemplateId);
        var folder = key switch
        {
            "tile-ceramic" => "/images/template-tile-ceramic/",
            "interior-decor" => "/images/template-interior-decor/",
            _ => "/images/template-home-appliances/",
        };
        Assert.All(preview.Categories.Where(c => c.ImageUrl is not null), c =>
        {
            Assert.StartsWith(folder, c.ImageUrl!, StringComparison.Ordinal);
            Assert.DoesNotContain("/images/fashion-template/", c.ImageUrl!, StringComparison.Ordinal);
            Assert.DoesNotContain("/images/template-auto-parts/", c.ImageUrl!, StringComparison.Ordinal);
        });
        Assert.Contains(preview.Products, p => p.MediaUrl != null && p.MediaUrl.StartsWith(folder, StringComparison.Ordinal));
        Assert.DoesNotContain(preview.Products, p => p.MediaUrl != null && p.MediaUrl.Contains("/images/fashion-template/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Structural_parity_is_seeded_once_per_pack()
    {
        await using var catalog = CreateCatalog();
        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));
        await IndustryBatchBTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

        Assert.Equal(2, await catalog.TemplateAttributeDefinitions.CountAsync(x => x.TemplateId == TileCeramicTemplateCatalogIds.TemplateId));
        Assert.Equal(2, await catalog.TemplateAttributeDefinitions.CountAsync(x => x.TemplateId == InteriorDecorTemplateCatalogIds.TemplateId));
        Assert.Equal(2, await catalog.TemplateAttributeDefinitions.CountAsync(x => x.TemplateId == HomeAppliancesTemplateCatalogIds.TemplateId));
        Assert.Equal(15, await catalog.TemplateVariants.CountAsync(x =>
            catalog.TemplateProducts.Any(p => p.ProductId == x.ProductId && p.TemplateId == TileCeramicTemplateCatalogIds.TemplateId)));
    }

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase("batch-b-t022-r12b-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private sealed class SimpleServiceProvider(CatalogDbContext catalog) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(CatalogDbContext) ? catalog : null;
    }
}
