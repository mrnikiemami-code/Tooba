using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Domain.ValueObjects;

namespace Tooba.Offer.Infrastructure.Adapters;

public sealed partial class OfferStore
{
    /// <inheritdoc />
    public async Task<OfferListPage> QueryAsync(OfferListCriteria criteria, CancellationToken cancellationToken)
    {
        var source = db.Offers.AsNoTracking()
            .Where(x => x.Status != OfferStatus.Archived);
        if (criteria.SellerPartyId is Guid sellerId)
        {
            source = source.Where(x => x.SellerPartyId == sellerId);
        }
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.SellerSku != null && x.SellerSku.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(criteria.Status)
            && Enum.TryParse<OfferStatus>(criteria.Status, ignoreCase: true, out var status))
        {
            source = source.Where(x => x.Status == status);
        }

        var total = await source.CountAsync(cancellationToken);
        source = criteria.SortField?.Trim().ToLowerInvariant() switch
        {
            "sellersku" => criteria.SortDescending
                ? source.OrderByDescending(x => x.SellerSku).ThenByDescending(x => x.OfferId)
                : source.OrderBy(x => x.SellerSku).ThenByDescending(x => x.OfferId),
            "status" => criteria.SortDescending
                ? source.OrderByDescending(x => x.Status).ThenByDescending(x => x.OfferId)
                : source.OrderBy(x => x.Status).ThenByDescending(x => x.OfferId),
            "lastupdatedat" or "updatedat" => criteria.SortDescending
                ? source.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.OfferId)
                : source.OrderBy(x => x.UpdatedAt).ThenByDescending(x => x.OfferId),
            _ => source.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.OfferId),
        };
        var items = await source
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);
        return new OfferListPage(items, total, criteria.Page, criteria.PageSize);
    }
}

