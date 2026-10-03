using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Brands;
using Tooba.Catalog.Application.Brands.Models;
using Tooba.Catalog.Application.Brands.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Focused Catalog persistence for Admin brand-options picker.</summary>
public sealed class BrandOptionReader : IBrandOptionReader
{
    private readonly CatalogDbContext _db;

    /// <summary>Creates the reader.</summary>
    public BrandOptionReader(CatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BrandOptionView>>> ListAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var brands = await _db.Brands.AsNoTracking().ToListAsync(cancellationToken);
        if (brands.Count == 0)
        {
            return Result.Success<IReadOnlyList<BrandOptionView>>([]);
        }

        var brandIds = brands.Select(b => b.BrandId).ToList();
        var nameRows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Brand
                && x.FieldKey == "name"
                && brandIds.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);

        var preferred = BrandOptionListBuilder.PreferredNames(
            nameRows.Select(x => (x.OwnerId, x.Locale, x.Value)));
        var rows = brands
            .Select(b => (b.BrandId, b.SlugSeam, b.Status.ToString()))
            .ToList();
        return Result.Success(BrandOptionListBuilder.Build(rows, preferred, search));
    }
}
