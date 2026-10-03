using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// اعتبارسنجی placement مگامنو — جدا از درخت taxonomy.
/// </summary>
public static class CatalogMegaMenuTreeRules
{
    /// <summary>حداکثر عمق presentation (L1/L2/L3).</summary>
    public const int MaxPresentationDepth = 3;

    /// <summary>
    /// والد و عمق presentation را بررسی می‌کند؛ expected failure به‌صورت enum برمی‌گردد.
    /// </summary>
    public static CatalogMegaMenuPlacementViolation ValidatePlacement(
        Guid megaMenuItemId,
        Guid? parentMegaMenuItemId,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItem> itemsById)
    {
        if (parentMegaMenuItemId is null)
        {
            return CatalogMegaMenuPlacementViolation.None;
        }

        if (parentMegaMenuItemId == megaMenuItemId)
        {
            return CatalogMegaMenuPlacementViolation.SelfParent;
        }

        if (!itemsById.ContainsKey(parentMegaMenuItemId.Value))
        {
            return CatalogMegaMenuPlacementViolation.ParentMissing;
        }

        var depth = 1;
        var current = parentMegaMenuItemId.Value;
        var seen = new HashSet<Guid> { megaMenuItemId };
        while (true)
        {
            if (!seen.Add(current))
            {
                return CatalogMegaMenuPlacementViolation.Cycle;
            }

            depth++;
            if (depth > MaxPresentationDepth)
            {
                return CatalogMegaMenuPlacementViolation.MaxDepthExceeded;
            }

            if (!itemsById.TryGetValue(current, out var parent) || parent.ParentMegaMenuItemId is not Guid next)
            {
                break;
            }

            current = next;
        }

        return CatalogMegaMenuPlacementViolation.None;
    }
}
