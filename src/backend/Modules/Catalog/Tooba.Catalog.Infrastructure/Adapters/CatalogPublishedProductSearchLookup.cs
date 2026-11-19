using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Adapters;

/// <summary>جستجو/صفحه‌بندی محصولات Published برای مصرف‌کنندگان Contracts.</summary>
public sealed class CatalogPublishedProductSearchLookup(CatalogDbContext catalog)
    : ICatalogPublishedProductSearchLookup
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<CatalogPublishedProductOption> Items, int TotalCount)> SearchPublishedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;

        var term = (search ?? string.Empty).Trim();
        var published = catalog.Products.AsNoTracking()
            .Where(p => p.Status == CatalogPublicationStatus.Published);

        if (term.Length > 0)
        {
            var matchedIds = catalog.LocalizedTexts.AsNoTracking()
                .Where(t =>
                    t.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && t.FieldKey == "name"
                    && t.Value.ToLower().Contains(term.ToLower()))
                .Select(t => t.OwnerId);
            published = published.Where(p => matchedIds.Contains(p.ProductId));
        }

        var total = await published.CountAsync(cancellationToken);
        var pageIds = await published
            .OrderBy(p => p.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ProductId)
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0)
            return (Array.Empty<CatalogPublishedProductOption>(), total);

        var titles = await catalog.LocalizedTexts.AsNoTracking()
            .Where(x =>
                x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.FieldKey == "name"
                && pageIds.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var titleMap = titles.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);

        var items = pageIds
            .Where(titleMap.ContainsKey)
            .Select(id => new CatalogPublishedProductOption(id, titleMap[id]))
            .ToList();
        return (items, total);
    }
}
