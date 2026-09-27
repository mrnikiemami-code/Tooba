using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Brands.Models;

namespace Tooba.Catalog.Application.Brands.Ports;

/// <summary>Focused Catalog read seam for Admin brand-options list.</summary>
public interface IBrandOptionReader
{
    /// <summary>
    /// Lists brand options with localized name preference, optional search, Ordinal sort, max 200.
    /// Always succeeds (empty list when no brands).
    /// </summary>
    Task<Result<IReadOnlyList<BrandOptionView>>> ListAsync(
        string? search,
        CancellationToken cancellationToken);
}
