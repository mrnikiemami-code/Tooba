using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// حل facet مؤثر رده با ارث والدین؛ eligibility از schema مؤثر IsFilterable.
/// </summary>
public static class CatalogCategoryFacetResolver
{
    /// <summary>
    /// حل facet مؤثر رده با ارث والدین و eligibility از schema.
    /// </summary>
    public static IReadOnlyList<CatalogEffectiveFacetBinding> ResolveEffectiveFacets(
        Guid categoryId,
        IReadOnlyDictionary<Guid, CatalogCategory> categoriesById,
        IReadOnlyList<CatalogCategoryFacetConfiguration> allConfigurations,
        IReadOnlyList<CatalogEffectiveSchemaBinding> effectiveSchema,
        IReadOnlyDictionary<Guid, CatalogAttributeDefinition> definitionsById)
    {
        ArgumentNullException.ThrowIfNull(categoriesById);
        ArgumentNullException.ThrowIfNull(allConfigurations);
        ArgumentNullException.ThrowIfNull(effectiveSchema);
        ArgumentNullException.ThrowIfNull(definitionsById);

        if (!categoriesById.ContainsKey(categoryId))
        {
            throw new InvalidOperationException("رده برای حل facet در Catalog این Tenant نیست.");
        }

        var filterable = effectiveSchema.Where(x => x.IsFilterable).ToDictionary(x => x.DefinitionId);
        if (filterable.Count == 0)
        {
            return Array.Empty<CatalogEffectiveFacetBinding>();
        }

        var ancestry = WalkAncestry(categoryId, categoriesById);
        var merged = new Dictionary<Guid, CatalogCategoryFacetConfiguration>();
        var sourceCategory = new Dictionary<Guid, Guid>();
        foreach (var ancestorId in ancestry)
        {
            foreach (var config in allConfigurations
                         .Where(c => c.CategoryId == ancestorId)
                         .OrderBy(c => c.SortOrder)
                         .ThenBy(c => c.FacetConfigurationId))
            {
                if (!filterable.ContainsKey(config.DefinitionId))
                {
                    continue;
                }

                merged[config.DefinitionId] = config;
                sourceCategory[config.DefinitionId] = ancestorId;
            }
        }

        return merged.Values
            .Select(config =>
            {
                if (!definitionsById.TryGetValue(config.DefinitionId, out var definition))
                {
                    throw new InvalidOperationException("تعریف ویژگی facet در Catalog نیست.");
                }

                return new CatalogEffectiveFacetBinding(
                    config.DefinitionId,
                    config.DisplayType,
                    config.SortOrder,
                    config.IsVisible,
                    config.IsSearchable,
                    config.IsCollapsedByDefault,
                    config.ShowCounts,
                    sourceCategory[config.DefinitionId],
                    definition);
            })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Guid> WalkAncestry(Guid categoryId, IReadOnlyDictionary<Guid, CatalogCategory> categoriesById)
    {
        var chain = new List<Guid>();
        var seen = new HashSet<Guid>();
        var current = categoryId;
        while (true)
        {
            if (!seen.Add(current))
            {
                throw new InvalidOperationException("حلقه در درخت ردهٔ Catalog تشخیص داده شد؛ facet قابل حل نیست.");
            }

            chain.Add(current);
            if (!categoriesById.TryGetValue(current, out var category) || category.ParentCategoryId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        chain.Reverse();
        return chain;
    }
}
