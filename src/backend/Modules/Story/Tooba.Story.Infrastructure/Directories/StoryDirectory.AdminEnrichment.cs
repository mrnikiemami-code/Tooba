using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Story.Infrastructure.Directories;

public sealed partial class StoryDirectory
{
    private async Task<AdminStorySnapshot> EnrichForCommandAsync(
        StoryEntity story,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var snapshot = MapAdmin(story, isAdmin);
        try
        {
            return await EnrichAsync(snapshot, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return snapshot;
        }
    }

    private async Task<(
        IReadOnlyDictionary<Guid, string> Paths,
        IReadOnlyDictionary<Guid, string> Titles)> ResolveAdminItemFactsAsync(
        IReadOnlyCollection<AdminStoryItemSnapshot> items,
        CancellationToken cancellationToken)
    {
        var productIds = new HashSet<Guid>();
        foreach (var item in items)
            CollectProductCta(item.CtaType, item.CtaTarget, productIds);

        var paths = new Dictionary<Guid, string>();
        if (_products is not null && productIds.Count > 0)
        {
            if (_products is IStoryProductPathBatchPort batch)
            {
                var resolved = await batch.ResolvePublicProductPathsAsync(productIds, cancellationToken);
                foreach (var pair in resolved)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Value))
                        paths[pair.Key] = pair.Value;
                }
            }
            else
            {
                foreach (var productId in productIds)
                {
                    var path = await _products.TryResolvePublicProductPathAsync(productId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(path))
                        paths[productId] = path;
                }
            }
        }

        IReadOnlyDictionary<Guid, string> titles = new Dictionary<Guid, string>();
        if (_productTitles is not null && productIds.Count > 0)
            titles = await _productTitles.GetProductTitlesByIdsAsync(productIds, cancellationToken);

        return (paths, titles);
    }

    private static IReadOnlyList<AdminStoryItemSnapshot> ApplyAdminItemFacts(
        IReadOnlyList<AdminStoryItemSnapshot> items,
        IReadOnlyDictionary<Guid, string> paths,
        IReadOnlyDictionary<Guid, string> titles)
    {
        return items.Select(item =>
        {
            var href = ResolveCtaHref(item.CtaType, item.CtaTarget, item.CtaHref, paths);
            if (!string.Equals(item.CtaType, "product", StringComparison.OrdinalIgnoreCase))
                return item with { CtaHref = href };
            if (!Guid.TryParse(item.CtaTarget?.Trim(), out var productId) || productId == Guid.Empty)
                return item with { CtaProductTitle = null, CtaHref = href };
            titles.TryGetValue(productId, out var title);
            return item with
            {
                CtaProductTitle = string.IsNullOrWhiteSpace(title) ? null : title,
                CtaHref = href,
            };
        }).ToList();
    }
}
