using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain.Aggregates;
using Tooba.Catalog.Domain.Enums;
using Tooba.Catalog.Infrastructure.Adapters;
using Tooba.Catalog.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// R5 C: owning CatalogPublishedProductSearchLookup query proof (not a fake port algorithm).
/// Controlled EF InMemory on the REAL Catalog lookup; ZERO committed rows.
/// </summary>
public sealed class CatalogPublishedProductSearch282R5Tests
{
    [Fact]
    public async Task Real_lookup_pages_55_published_finds_beyond_page_one_and_excludes_nonpublic()
    {
        var dbName = "catalog-pub-search-r5-" + Guid.NewGuid().ToString("N");
        await using var db = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>().UseInMemoryDatabase(dbName).Options);
        var now = DateTimeOffset.UtcNow;

        var published = new List<CatalogProduct>();
        for (var i = 0; i < 55; i++)
        {
            var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, $"p-{i:D2}", now);
            product.Publish(now);
            published.Add(product);
            db.Products.Add(product);
        }

        for (var i = 0; i < 5; i++)
        {
            var draft = CatalogProduct.Create(CatalogProductKind.PhysicalGood, $"draft-{i}", now);
            db.Products.Add(draft);
            db.LocalizedTexts.Add(CatalogLocalizedText.Create(
                CatalogLocalizedOwnerKind.Product, draft.ProductId, "name", "fa-IR", $"پیش‌نویس {i}"));
        }

        var ordered = published.OrderBy(p => p.ProductId).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var title = i == 41 ? "کالای دور صفحه دو" : $"محصول منتشر {i:D2}";
            db.LocalizedTexts.Add(CatalogLocalizedText.Create(
                CatalogLocalizedOwnerKind.Product, ordered[i].ProductId, "name", "fa-IR", title));
        }

        await db.SaveChangesAsync();

        var sut = new CatalogPublishedProductSearchLookup(db);
        var page1 = await sut.SearchPublishedAsync(search: null, page: 1, pageSize: 20, CancellationToken.None);
        var page2 = await sut.SearchPublishedAsync(search: null, page: 2, pageSize: 20, CancellationToken.None);
        var page3 = await sut.SearchPublishedAsync(search: null, page: 3, pageSize: 20, CancellationToken.None);
        var found = await sut.SearchPublishedAsync(search: "دور صفحه", page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(55, page1.TotalCount);
        Assert.Equal(20, page1.Items.Count);
        Assert.Equal(20, page2.Items.Count);
        Assert.Equal(15, page3.Items.Count);
        Assert.Empty(page1.Items.Select(x => x.ProductId).Intersect(page2.Items.Select(x => x.ProductId)));
        Assert.DoesNotContain(page1.Items, x => x.Title.Contains("دور صفحه", StringComparison.Ordinal));
        Assert.Contains(found.Items, x => x.Title == "کالای دور صفحه دو");
        Assert.Equal(1, found.TotalCount);
        Assert.DoesNotContain(page1.Items.Concat(page2.Items).Concat(page3.Items), x => x.Title.StartsWith("پیش‌نویس", StringComparison.Ordinal));
    }
}
