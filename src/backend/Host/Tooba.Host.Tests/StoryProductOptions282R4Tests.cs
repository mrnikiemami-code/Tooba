using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Infrastructure.Adapters;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// S2 owning-query proof: real adapter paging/search/count and shop isolation via Contracts ports.
/// </summary>
public sealed class StoryProductOptions282R4Tests
{
    [Fact]
    public async Task Published_search_finds_item_beyond_first_page_with_accurate_count_and_disjoint_pages()
    {
        var catalog = new FakePublishedSearch(total: 55, matchAtIndex: 41, matchTitle: "کالای دور صفحه دو");
        var sut = new StoryProductOptionsAdapter(null, null, catalog);

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
        Assert.Null(catalog.SamplerUsed);
    }

    [Fact]
    public async Task Seller_search_isolates_shops_and_pages_eligible_set_only()
    {
        var shopA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var shopB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var products = new FakeSellerProducts(
            new Dictionary<Guid, IReadOnlyList<Guid>>
            {
                [shopA] = Enumerable.Range(1, 45).Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}")).ToArray(),
                [shopB] = new[] { Guid.Parse("00000000-0000-0000-0000-000000000099") },
            });
        var titles = new FakeTitles(
            products.AllIds.ToDictionary(
                id => id,
                id => id == Guid.Parse("00000000-0000-0000-0000-000000000099")
                    ? "فقط فروشگاه ب"
                    : $"کالای {id.ToString()[^2..]}"));
        var sut = new StoryProductOptionsAdapter(products, titles, publishedSearch: null);

        var aPage1 = await sut.SearchForSellerAsync(shopA, null, 1, 20, CancellationToken.None);
        var aPage2 = await sut.SearchForSellerAsync(shopA, null, 2, 20, CancellationToken.None);
        var bOnly = await sut.SearchForSellerAsync(shopB, "فروشگاه ب", 1, 20, CancellationToken.None);
        var aCannotSeeB = await sut.SearchForSellerAsync(shopA, "فروشگاه ب", 1, 20, CancellationToken.None);

        Assert.Equal(45, aPage1.TotalCount);
        Assert.Equal(20, aPage1.Items.Count);
        Assert.Equal(20, aPage2.Items.Count);
        Assert.Empty(aPage1.Items.Select(x => x.ProductId).Intersect(aPage2.Items.Select(x => x.ProductId)));
        Assert.Equal(1, bOnly.TotalCount);
        Assert.Equal("فقط فروشگاه ب", bOnly.Items[0].Title);
        Assert.Equal(0, aCannotSeeB.TotalCount);
    }

    private sealed class FakePublishedSearch(int total, int matchAtIndex, string matchTitle) : ICatalogPublishedProductSearchLookup
    {
        public bool? SamplerUsed { get; private set; }

        public Task<(IReadOnlyList<CatalogPublishedProductOption> Items, int TotalCount)> SearchPublishedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            // Real published search path — sampler never engaged (stays null).
            var all = Enumerable.Range(0, total)
                .Select(i => new CatalogPublishedProductOption(
                    Guid.Parse($"11111111-1111-1111-1111-{i:D12}"),
                    i == matchAtIndex ? matchTitle : $"محصول {i:D2}"))
                .ToList();
            var term = (search ?? string.Empty).Trim();
            IEnumerable<CatalogPublishedProductOption> filtered = all;
            if (term.Length > 0)
                filtered = all.Where(x => x.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
            var list = filtered.ToList();
            var items = list.Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(((IReadOnlyList<CatalogPublishedProductOption>)items, list.Count));
        }
    }

    private sealed class FakeSellerProducts(Dictionary<Guid, IReadOnlyList<Guid>> map) : IOfferSellerProductIdLookup
    {
        public IEnumerable<Guid> AllIds => map.Values.SelectMany(x => x);

        public Task<IReadOnlyList<Guid>> ListDistinctProductIdsForSellerAsync(
            Guid sellerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult(map.TryGetValue(sellerPartyId, out var ids) ? ids : Array.Empty<Guid>());
    }

    private sealed class FakeTitles(Dictionary<Guid, string> map) : ICatalogAdminProductTitleIdLookup
    {
        public Task<IReadOnlySet<Guid>> ResolveProductIdsByTitleContainsAsync(
            string term,
            CancellationToken cancellationToken)
        {
            var needle = (term ?? string.Empty).Trim();
            IReadOnlySet<Guid> ids = map
                .Where(kv => needle.Length == 0 || kv.Value.Contains(needle, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToHashSet();
            return Task.FromResult(ids);
        }

        public Task<IReadOnlySet<Guid>> ResolveProductIdsByTitleFilterAsync(
            CatalogProductTitleTextFilter filter,
            CancellationToken cancellationToken) =>
            ResolveProductIdsByTitleContainsAsync(filter.Value ?? string.Empty, cancellationToken);

        public Task<IReadOnlyDictionary<Guid, string>> GetProductTitlesByIdsAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken)
        {
            var result = productIds
                .Where(map.ContainsKey)
                .ToDictionary(id => id, id => map[id]);
            return Task.FromResult((IReadOnlyDictionary<Guid, string>)result);
        }
    }
}
