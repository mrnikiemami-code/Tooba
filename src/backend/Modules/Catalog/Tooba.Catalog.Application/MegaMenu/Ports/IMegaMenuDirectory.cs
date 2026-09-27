using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.MegaMenu.Ports;

/// <summary>Catalog-owned port for MegaMenu Admin + Storefront operations.</summary>
public interface IMegaMenuDirectory
{
    /// <summary>Admin configuration for a category; missing category → catalog.megamenu.category.missing.</summary>
    Task<Result<CategoryMegaMenuConfigurationView>> GetCategoryConfigurationAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>Placement options for Admin selector (excludes the category itself).</summary>
    Task<IReadOnlyList<MegaMenuPlacementOption>> ListPlacementOptionsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>Creates or updates category MegaMenu binding + optional translation overrides.</summary>
    Task<Result> UpsertBindingAsync(
        Guid categoryId,
        string locale,
        CategoryMegaMenuBindingInput input,
        CancellationToken cancellationToken);

    /// <summary>Removes MegaMenu binding for a category (idempotent when absent).</summary>
    Task<Result> RemoveBindingAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>Composed storefront MegaMenu items for locale.</summary>
    Task<IReadOnlyList<StorefrontMegaMenuItem>> GetStorefrontMenuAsync(
        string locale,
        CancellationToken cancellationToken);
}
