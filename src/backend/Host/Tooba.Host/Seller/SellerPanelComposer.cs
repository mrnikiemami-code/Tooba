using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Party.Application;

namespace Tooba.Host.Seller;

/// <summary>Composes non-Order seller panel surfaces (catalog variants) and thin dashboard shell.</summary>
public sealed class SellerPanelComposer(
    CatalogDbContext catalog,
    IPartyLookupGateway parties)
{
    /// <summary>
    /// Builds seller display shell for dashboard. Order counts come from Order CQRS via endpoints.
    /// </summary>
    public async Task<(string DisplayName, bool SellerFound)> GetSellerDisplayAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var seller = await parties.FindByIdAsync(sellerPartyId, cancellationToken);
        if (seller is null)
        {
            return (string.Empty, false);
        }

        return (seller.DisplayName, true);
    }

    /// <summary>Lists published Catalog variants available for seller selection.</summary>
    public async Task<IReadOnlyList<SellerCatalogVariantOption>> ListCatalogVariantsAsync(
        Guid sellerPartyId, CancellationToken cancellationToken)
    {
        if (await parties.FindByIdAsync(sellerPartyId, cancellationToken) is null)
        {
            throw new PlatformHttpException(404, "Seller was not found.", "seller.missing");
        }

        var products = await catalog.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published)
            .OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(cancellationToken);
        var ids = products.Select(x => x.ProductId).ToArray();
        var names = await catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && ids.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var nameMap = names.GroupBy(x => x.OwnerId).ToDictionary(
            x => x.Key, x => x.OrderBy(y => y.Locale.StartsWith("fa") ? 0 : 1).First().Value);
        var variants = await catalog.Variants.AsNoTracking()
            .Where(x => ids.Contains(x.ProductId)).OrderBy(x => x.CatalogCodeSeam).ToListAsync(cancellationToken);
        var statuses = products.ToDictionary(x => x.ProductId, x => x.Status.ToString());
        return variants.Select(x => new SellerCatalogVariantOption(
            x.VariantId, x.ProductId, nameMap.GetValueOrDefault(x.ProductId) ?? string.Empty,
            x.CatalogCodeSeam, statuses.GetValueOrDefault(x.ProductId) ?? "Published")).ToArray();
    }
}
