using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Admin;

/// <summary>پیاده‌سازی Contracts برای resolve شناسه محصول از روی عنوان محلی.</summary>
public sealed class CatalogAdminProductTitleIdLookup(CatalogDbContext catalog) : ICatalogAdminProductTitleIdLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ResolveProductIdsByTitleContainsAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var ids = await catalog.LocalizedTexts.AsNoTracking()
            .Where(t =>
                t.OwnerKind == CatalogLocalizedOwnerKind.Product
                && t.FieldKey == "name"
                && t.Value.ToLower().Contains(term.ToLower()))
            .Select(t => t.OwnerId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ResolveProductIdsByTitleFilterAsync(
        CatalogProductTitleTextFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var op = (filter.Operator ?? string.Empty).Trim();

        if (op is "blank")
        {
            var withTitle = await catalog.LocalizedTexts.AsNoTracking()
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name" && t.Value != "")
                .Select(t => t.OwnerId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var allProducts = await catalog.Products.AsNoTracking().Select(p => p.ProductId).ToListAsync(cancellationToken);
            return allProducts.Except(withTitle).ToHashSet();
        }

        if (op is "notBlank")
        {
            return (await catalog.LocalizedTexts.AsNoTracking()
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name" && t.Value != "")
                .Select(t => t.OwnerId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        var q = catalog.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name");
        var value = filter.Value ?? string.Empty;
        q = op switch
        {
            "equals" => q.Where(t => t.Value.ToLower() == value.ToLower()),
            "notEqual" => q.Where(t => t.Value.ToLower() != value.ToLower()),
            "notContains" => q.Where(t => !t.Value.ToLower().Contains(value.ToLower())),
            "startsWith" => q.Where(t => t.Value.ToLower().StartsWith(value.ToLower())),
            "endsWith" => q.Where(t => t.Value.ToLower().EndsWith(value.ToLower())),
            _ => q.Where(t => t.Value.ToLower().Contains(value.ToLower())),
        };

        return (await q.Select(t => t.OwnerId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
    }
}
