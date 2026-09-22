using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Host.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Models;
using Tooba.AddressBook.Contracts;
using Tooba.Cart.Application.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class FashionTemplateCatalogT022R6Tests
{
    [Fact]
    public async Task Structural_parity_seed_is_idempotent_and_fills_deferred_mirrors()
    {
        await using var catalog = CreateCatalog();
        var provider = new SimpleServiceProvider(catalog);

        await FashionTemplateCatalogSeed.ApplyAsync(provider);
        await FashionTemplateCatalogSeed.ApplyAsync(provider);

        Assert.Equal(2, await catalog.TemplateAttributeDefinitions.CountAsync(x => x.TemplateId == FashionTemplateCatalogIds.TemplateId));
        Assert.Equal(3, await catalog.TemplateAttributeOptions.CountAsync());
        Assert.Equal(2, await catalog.TemplateTags.CountAsync(x => x.TemplateId == FashionTemplateCatalogIds.TemplateId));
        Assert.Equal(8, await catalog.TemplateCategoryAttributeBindings.CountAsync());
        Assert.Equal(8, await catalog.TemplateCategoryFacetConfigurations.CountAsync());
        Assert.Equal(8, await catalog.TemplateMegaMenuItems.CountAsync());
        Assert.Equal(8, await catalog.TemplateMegaMenuItemTranslations.CountAsync());
        Assert.Equal(8, await catalog.TemplateCategorySlugHistories.CountAsync());
        Assert.Equal(8, await catalog.TemplateCategoryTagAssignments.CountAsync());
        Assert.Equal(15, await catalog.TemplateProductAttributeValues.CountAsync());
        Assert.Equal(15, await catalog.TemplateProductTagAssignments.CountAsync());
        Assert.Equal(15, await catalog.TemplateProductVariantAxes.CountAsync());
        Assert.Equal(15, await catalog.TemplateVariants.CountAsync());
        Assert.Equal(15, await catalog.TemplateVariantAttributeValues.CountAsync());
        Assert.Equal(15, await catalog.TemplateProductHistoryEntries.CountAsync());
        Assert.True(await catalog.TemplateLocalizedTexts.AnyAsync(x => x.OwnerKind == TemplateLocalizedOwnerKind.Tag));
        Assert.True(await catalog.TemplateLocalizedTexts.AnyAsync(x => x.OwnerKind == TemplateLocalizedOwnerKind.AttributeDefinition));
        Assert.True(await catalog.TemplateLocalizedTexts.AnyAsync(x => x.OwnerKind == TemplateLocalizedOwnerKind.AttributeOption));
    }

    [Fact]
    public async Task Preview_categories_expose_template_media_urls()
    {
        await using var catalog = CreateCatalog();
        await FashionTemplateCatalogSeed.ApplyAsync(new SimpleServiceProvider(catalog));

        var preview = await new FashionTemplatePreviewQuery(catalog).GetFashionSampleAsync();
        Assert.NotNull(preview);
        Assert.True(preview!.Purity.IsPure);
        Assert.Equal(15, preview.Products.Count);
        Assert.Equal(8, preview.Purity.TemplateTopLevelCategoryCount);

        var roots = preview.Categories.Where(c => c.ParentCategoryId is null).ToList();
        Assert.Equal(8, roots.Count);
        Assert.All(roots, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.ImageMediaAssetId));
            Assert.False(string.IsNullOrWhiteSpace(c.ImageUrl));
            Assert.Contains("/images/fashion-template/", c.ImageUrl!, StringComparison.Ordinal);
            Assert.DoesNotContain("/images/categories/", c.ImageUrl!, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Operational_catalog_product_family_still_has_no_template_ownership()
    {
        foreach (var type in new[] { typeof(CatalogProduct), typeof(CatalogCategory), typeof(CatalogBrand) })
        {
            var names = type.GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            Assert.DoesNotContain("TemplateId", names);
            Assert.DoesNotContain("IsDemo", names);
        }

        Assert.Contains(typeof(TemplateAttributeDefinition).GetProperties().Select(p => p.Name), n => n == "TemplateId");
        Assert.Contains(typeof(TemplateTag).GetProperties().Select(p => p.Name), n => n == "TemplateId");
    }

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase("fashion-t022-r6-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private sealed class SimpleServiceProvider(CatalogDbContext catalog) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(CatalogDbContext) ? catalog : null;
    }
}
