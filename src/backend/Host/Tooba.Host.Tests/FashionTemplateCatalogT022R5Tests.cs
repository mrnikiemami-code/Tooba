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

public sealed class FashionTemplateCatalogT022R5Tests
{
    [Fact]
    public async Task Seed_is_idempotent_and_matches_exact_counts()
    {
        await using var catalog = CreateCatalog();
        var provider = new SimpleServiceProvider(catalog);

        await FashionTemplateCatalogSeed.ApplyAsync(provider);
        await FashionTemplateCatalogSeed.ApplyAsync(provider);

        Assert.Equal(1, await catalog.StoreTemplates.CountAsync());
        Assert.Equal(15, await catalog.TemplateProducts.CountAsync(x => x.TemplateId == FashionTemplateCatalogIds.TemplateId));
        Assert.Equal(6, await catalog.TemplateBrands.CountAsync(x => x.TemplateId == FashionTemplateCatalogIds.TemplateId));

        var roots = await catalog.TemplateCategories
            .Where(x => x.TemplateId == FashionTemplateCatalogIds.TemplateId && x.ParentCategoryId == null)
            .ToListAsync();
        Assert.Equal(8, roots.Count);

        foreach (var root in roots)
        {
            var mids = await catalog.TemplateCategories
                .Where(x => x.ParentCategoryId == root.CategoryId)
                .ToListAsync();
            Assert.True(mids.Count >= 1);
            foreach (var mid in mids)
            {
                var leaves = await catalog.TemplateCategories.CountAsync(x => x.ParentCategoryId == mid.CategoryId);
                Assert.True(leaves >= 1);
            }
        }

        Assert.Equal(
            await catalog.TemplateProducts.CountAsync(),
            await catalog.TemplateProductMediaReferences.CountAsync());
        Assert.Equal(
            await catalog.TemplateProducts.CountAsync(),
            await catalog.TemplateLocalizedTexts.CountAsync(x => x.OwnerKind == TemplateLocalizedOwnerKind.Product));
        Assert.True(await catalog.TemplateStoreLandingPageSections.AnyAsync(x => x.SectionType == "BannerShowcase"));
    }

    [Fact]
    public async Task Preview_query_is_pure_template_catalog_only()
    {
        await using var catalog = CreateCatalog();
        await FashionTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

        // Contaminate operational tables with unrelated IDs — must not appear in preview.
        catalog.Products.Add(CatalogProduct.Create(CatalogProductKind.PhysicalGood, "ops-only", DateTimeOffset.UtcNow));
        catalog.Categories.Add(CatalogCategory.Create(null, DateTimeOffset.UtcNow));
        catalog.Brands.Add(CatalogBrand.Create("ops-brand", DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();

        var query = new FashionTemplatePreviewQuery(catalog);
        var preview = await query.GetFashionSampleAsync();
        Assert.NotNull(preview);
        Assert.Equal(15, preview!.Products.Count);
        Assert.Equal(8, preview.Purity.TemplateTopLevelCategoryCount);
        Assert.Equal(0, preview.Purity.OperationalProductIdHits);
        Assert.Equal(0, preview.Purity.OperationalCategoryIdHits);
        Assert.Equal(0, preview.Purity.OperationalBrandIdHits);
        Assert.True(preview.Purity.IsPure);
        Assert.Equal(FashionTemplateCatalogSeed.Origin, preview.Origin);
        Assert.DoesNotContain(preview.Products, p => p.Slug.Contains("ops", StringComparison.Ordinal));
    }

    [Fact]
    public void Operational_product_type_has_no_template_ownership_members()
    {
        var names = typeof(CatalogProduct).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("TemplateId", names);
        Assert.DoesNotContain("IsDemo", names);
        Assert.DoesNotContain("DataScope", names);
        Assert.Contains(typeof(TemplateProduct).GetProperties().Select(p => p.Name), n => n == "TemplateId");
    }

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase("fashion-t022-r5-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private sealed class SimpleServiceProvider(CatalogDbContext catalog) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(CatalogDbContext) ? catalog : null;
    }
}
