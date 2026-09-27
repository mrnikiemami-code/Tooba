using Tooba.Catalog.Application.Brands.Models;

namespace Tooba.Catalog.Application.Brands;

/// <summary>
/// Pure Admin brand-options projection/filter/sort/limit (Host W20 evacuate parity).
/// </summary>
public static class BrandOptionListBuilder
{
    /// <summary>
    /// Builds option rows. <paramref name="preferredNames"/> already applies fa-IR then locale preference (first wins).
    /// </summary>
    public static IReadOnlyList<BrandOptionView> Build(
        IReadOnlyList<(Guid BrandId, string? SlugSeam, string Status)> brands,
        IReadOnlyDictionary<Guid, string> preferredNames,
        string? search)
    {
        if (brands.Count == 0)
        {
            return [];
        }

        IEnumerable<BrandOptionView> items = brands.Select(b =>
            new BrandOptionView(
                b.BrandId,
                preferredNames.GetValueOrDefault(b.BrandId) ?? b.SlugSeam ?? "برند",
                b.Status));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            items = items.Where(i => i.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return items.OrderBy(i => i.Name, StringComparer.Ordinal).Take(200).ToList();
    }

    /// <summary>
    /// Collapses ordered localized name rows (fa-IR first, then locale ascending) to first name per brand.
    /// </summary>
    public static IReadOnlyDictionary<Guid, string> PreferredNames(
        IEnumerable<(Guid OwnerId, string Locale, string Value)> orderedNameRows) =>
        orderedNameRows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(g => g.Key, g => g.First().Value);
}
