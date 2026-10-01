using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Admin;
using Tooba.Party.Contracts;

namespace Tooba.Party.Application.Admin.Sellers.Queries;

/// <summary>
/// فهرست فروشندگان Admin — ترکیب Contracts-only Offer/Party/Order.
/// بدون payload ورودی؛ NO_VALIDATOR_REQUIRED.
/// </summary>
public sealed record ListAdminSellersQuery : IRequest<Result<IReadOnlyList<AdminSellerListItem>>>;

/// <summary>Handler ترکیب فهرست فروشندگان Admin با حفظ رفتار Host Panel قبلی.</summary>
public sealed class ListAdminSellersQueryHandler(
    IOfferQueryGateway offers,
    IPartyAdminSellerReadGateway parties,
    IAdminSellerOrderCountPort sellerOrderCounts)
    : IRequestHandler<ListAdminSellersQuery, Result<IReadOnlyList<AdminSellerListItem>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AdminSellerListItem>>> Handle(
        ListAdminSellersQuery request,
        CancellationToken cancellationToken)
    {
        var offerRows = await offers.ListSellerStatusRowsAsync(cancellationToken).ConfigureAwait(false);
        var sellerIds = offerRows.Select(x => x.SellerPartyId).Distinct().ToList();
        var partyRows = await parties.GetStatusProjectionsAsync(sellerIds, cancellationToken).ConfigureAwait(false);
        var orderMap = await sellerOrderCounts.GetCountsBySellerAsync(sellerIds, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AdminSellerListItem> items = partyRows.Select(party => new AdminSellerListItem(
            party.PartyId,
            party.DisplayName,
            party.Status,
            offerRows.Count(x => x.SellerPartyId == party.PartyId && x.Status == OfferStatus.Active),
            orderMap.GetValueOrDefault(party.PartyId))).ToList();
        return Result.Success(items);
    }
}
