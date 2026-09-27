using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.ProductWorkspace.Application.Composition.Queries;

/// <summary>Composes AdminProductListItem rows from Catalog slices + Offer/Price/Inventory Contracts.</summary>
internal static class ProductWorkspaceListComposer
{
    public static async Task<IReadOnlyList<AdminProductListItem>> BuildListItemsAsync(
        ICatalogAdminProductWorkspaceListGateway catalog,
        IOfferQueryGateway offers,
        IPriceQueryGateway prices,
        IInventoryQueryGateway inventory,
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var slices = await catalog.LoadListCatalogSlicesAsync(productIds, cancellationToken);
        var variantIds = slices.SelectMany(s => s.VariantIds).Distinct().ToList();
        var offerRows = variantIds.Count == 0
            ? []
            : (await offers.ListOffersByCatalogVariantIdsAsync(variantIds, cancellationToken))
                .Select(x => new { x.OfferId, x.CatalogVariantId })
                .ToList();
        var offerIds = offerRows.Select(x => x.OfferId).ToList();
        var amountRows = offerIds.Count == 0
            ? []
            : (await prices.ListByOfferIdsAsync(offerIds, cancellationToken))
                .Select(x => new { x.OfferId, x.Amount, x.Currency })
                .ToList();
        var unitRows = offerIds.Count == 0
            ? []
            : (await inventory.ListPositionsByOfferIdsAsync(offerIds, cancellationToken))
                .Select(x => new { x.OfferId, x.OnHand, x.Reserved, x.LocationId })
                .ToList();

        return slices.Select(slice =>
        {
            var productOffers = offerRows.Where(row => slice.VariantIds.Contains(row.CatalogVariantId)).ToList();
            var productOfferIds = productOffers.Select(row => row.OfferId).ToHashSet();
            var amounts = amountRows.Where(row => productOfferIds.Contains(row.OfferId)).ToList();
            var units = unitRows.Where(row => productOfferIds.Contains(row.OfferId)).ToList();
            return new AdminProductListItem(
                slice.ProductId,
                slice.Title,
                slice.Status,
                slice.VariantIds.Count,
                productOffers.Count,
                slice.CategorySummary,
                FormatOfferAmountRange(amounts.Select(row => (row.Amount, row.Currency)).ToList()),
                units.Sum(row => row.OnHand - row.Reserved),
                units.Select(row => row.LocationId).Distinct().Count(),
                slice.UpdatedAt,
                slice.PrimaryMediaAssetId,
                slice.PrimaryCategoryId,
                slice.BrandName,
                slice.PrimaryCategoryName,
                slice.AdditionalCategoryNames,
                slice.AdditionalCategoryCount);
        }).ToList();
    }

    private static string FormatOfferAmountRange(IReadOnlyList<(decimal Amount, string Currency)> rows)
    {
        if (rows.Count == 0)
        {
            return "بدون مبلغ";
        }

        var min = rows.Min(x => x.Amount);
        var max = rows.Max(x => x.Amount);
        var currency = rows[0].Currency;
        if (min == max)
        {
            return $"{min:0} {currency}".Trim();
        }

        return $"{min:0}–{max:0} {currency}".Trim();
    }
}
