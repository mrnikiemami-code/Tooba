using Tooba.Catalog.Application.Storefront.Ports;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Catalog.Infrastructure.Storefront;

namespace Tooba.Catalog.Infrastructure.Adapters;

/// <summary>انتساب عمومی فروشندهٔ ویترین برای مصرف‌کنندگان بین‌ماژولی.</summary>
public sealed class CatalogPublicSellerAttributionAdapter(IStorefrontComposer storefront)
    : ICatalogPublicSellerAttributionLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, CatalogPublicSellerAttribution>> ResolveAsync(
        IReadOnlyCollection<Guid> sellerPartyIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, CatalogPublicSellerAttribution>();
        foreach (var partyId in sellerPartyIds.Where(id => id != Guid.Empty).Distinct())
        {
            var publicId = StorefrontComposer.CreatePublicSellerId(partyId);
            var page = await storefront.GetPublicSellerAsync(publicId, cancellationToken);
            if (page is null)
                continue;
            result[partyId] = new CatalogPublicSellerAttribution(
                partyId,
                page.Seller.PublicId,
                page.Seller.DisplayName);
        }

        return result;
    }
}
