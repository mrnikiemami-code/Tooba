using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// حل schema مؤثر رده با ارث از والدین؛ حلقهٔ درخت تشخیص داده می‌شود.
/// </summary>
public static class CatalogCategorySchemaResolver
{
    /// <summary>
    /// از ردهٔ هدف به ریشه راه می‌رود، پیوندها را ادغام می‌کند (فرزند روی همان DefinitionId غالب است)،
    /// و فهرست مرتب با فرادادهٔ تعریف برمی‌گرداند.
    /// </summary>
    public static IReadOnlyList<CatalogEffectiveSchemaBinding> ResolveEffectiveSchema(
        Guid categoryId,
        IReadOnlyDictionary<Guid, CatalogCategory> categoriesById,
        IReadOnlyList<CatalogCategoryAttributeBinding> allBindings,
        IReadOnlyDictionary<Guid, CatalogAttributeDefinition> definitionsById)
    {
        ArgumentNullException.ThrowIfNull(categoriesById);
        ArgumentNullException.ThrowIfNull(allBindings);
        ArgumentNullException.ThrowIfNull(definitionsById);

        if (!categoriesById.ContainsKey(categoryId))
        {
            throw new InvalidOperationException("رده برای حل schema در Catalog این Tenant نیست.");
        }

        var ancestry = WalkAncestry(categoryId, categoriesById);
        // از ریشه به فرزند: فرزند override می‌کند.
        var merged = new Dictionary<Guid, CatalogCategoryAttributeBinding>();
        var inheritedFrom = new Dictionary<Guid, Guid>();
        var overriddenFrom = new Dictionary<Guid, Guid?>();
        foreach (var ancestorId in ancestry)
        {
            foreach (var binding in allBindings.Where(b => b.CategoryId == ancestorId)
                         .OrderBy(b => b.DisplayOrder)
                         .ThenBy(b => b.BindingId))
            {
                if (merged.ContainsKey(binding.DefinitionId))
                {
                    overriddenFrom[binding.DefinitionId] = inheritedFrom[binding.DefinitionId];
                }
                else if (!overriddenFrom.ContainsKey(binding.DefinitionId))
                {
                    overriddenFrom[binding.DefinitionId] = null;
                }

                merged[binding.DefinitionId] = binding;
                inheritedFrom[binding.DefinitionId] = ancestorId;
            }
        }

        return merged.Values
            .Select(binding =>
            {
                if (!definitionsById.TryGetValue(binding.DefinitionId, out var definition))
                {
                    throw new InvalidOperationException("تعریف ویژگی پیوندشده در Catalog نیست.");
                }

                var isVariantAxis = binding.IsVariantAxis && definition.IsVariantAxisAllowed;
                var sourceCategoryId = inheritedFrom[binding.DefinitionId];
                var priorAncestor = overriddenFrom.GetValueOrDefault(binding.DefinitionId);
                // override محلی فقط وقتی منبع نهایی خودِ رده است و قبلاً از والد آمده.
                var localOverrideFrom = sourceCategoryId == categoryId ? priorAncestor : null;
                return new CatalogEffectiveSchemaBinding(
                    binding.DefinitionId,
                    binding.DisplayOrder,
                    binding.IsRequired,
                    binding.IsFilterable,
                    isVariantAxis,
                    binding.IsComparable,
                    sourceCategoryId,
                    definition,
                    localOverrideFrom);
            })
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// مقادیر محصول و محورهایی که در schema جدید جایی ندارند را فهرست می‌کند؛ حذف نمی‌کند.
    /// </summary>
    public static CatalogCategoryChangeImpactReport PreviewCategoryChange(
        IReadOnlyList<CatalogProductAttributeValue> productValues,
        IReadOnlyList<CatalogProductVariantAxis> productAxes,
        IReadOnlyList<CatalogEffectiveSchemaBinding> newEffectiveSchema)
    {
        ArgumentNullException.ThrowIfNull(productValues);
        ArgumentNullException.ThrowIfNull(productAxes);
        ArgumentNullException.ThrowIfNull(newEffectiveSchema);

        var allowed = newEffectiveSchema.Select(x => x.DefinitionId).ToHashSet();
        var orphans = productValues
            .Where(v => !allowed.Contains(v.DefinitionId))
            .Select(v => (v.DefinitionId, v.CanonicalValue))
            .ToList();
        var invalidAxes = productAxes
            .Where(a => !allowed.Contains(a.DefinitionId))
            .Select(a => a.DefinitionId)
            .Distinct()
            .ToList();
        return new CatalogCategoryChangeImpactReport(orphans, invalidAxes);
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
                throw new InvalidOperationException("حلقه در درخت ردهٔ Catalog تشخیص داده شد؛ schema قابل حل نیست.");
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
