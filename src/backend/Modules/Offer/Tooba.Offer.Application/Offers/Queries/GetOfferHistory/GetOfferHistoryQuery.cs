using System.Globalization;
using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts.Seller;

namespace Tooba.Offer.Application.Offers.Queries.GetOfferHistory;

/// <summary>
/// Authorized read of Owner-recorded history. Seller-scoped callers must own the offer; admin callers
/// pass no seller id. Price/Inventory facts come from their own durable transactions.
/// </summary>
public sealed record GetOfferHistoryQuery(Guid OfferId, Guid? SellerPartyId, int Page, int PageSize)
    : IRequest<Result<OfferHistoryPage>>;

internal sealed class GetOfferHistoryHandler(
    IOfferStore store,
    IOfferHistoryReader history,
    IOfferPriceHistoryReader priceHistory,
    IOfferInventoryHistoryReader inventoryHistory)
    : IRequestHandler<GetOfferHistoryQuery, Result<OfferHistoryPage>>
{
    public async Task<Result<OfferHistoryPage>> Handle(
        GetOfferHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null)
            return OfferReadModelComposer.NotFound<OfferHistoryPage>();
        if (request.SellerPartyId is Guid sellerId && offer.SellerPartyId != sellerId)
            return OfferReadModelComposer.NotFound<OfferHistoryPage>();

        var items = new List<OfferHistoryItem>();
        items.AddRange(await history.ListAsync(request.OfferId, cancellationToken));
        foreach (var fact in await priceHistory.ListAsync(request.OfferId, cancellationToken))
        {
            items.Add(new OfferHistoryItem(
                fact.OfferId,
                fact.OccurredAt,
                "PriceChanged",
                fact.ActorUserId,
                "PriceChanged",
                fact.OldAmount is null ? null : FormattableString.Invariant($"amount={fact.OldAmount};currency={fact.Currency}"),
                FormattableString.Invariant($"amount={fact.NewAmount};currency={fact.Currency}"), fact.FactId, fact.PriceId));
        }
        foreach (var fact in await inventoryHistory.ListAsync(request.OfferId, cancellationToken))
        {
            if (fact.Kind == "AllocationChanged")
            {
                items.Add(new OfferHistoryItem(
                    fact.OfferId, fact.OccurredAt, "AllocationChanged", fact.ActorUserId,
                    "AllocationChanged",
                    fact.OldAllocation?.ToString(CultureInfo.InvariantCulture),
                    fact.NewAllocation?.ToString(CultureInfo.InvariantCulture), fact.FactId, fact.OfferId));
            }
            else
            {
                items.Add(new OfferHistoryItem(
                    fact.OfferId, fact.OccurredAt, "InventoryChanged", fact.ActorUserId,
                    "InventoryChanged",
                    fact.OldOnHand?.ToString(CultureInfo.InvariantCulture),
                    fact.NewOnHand?.ToString(CultureInfo.InvariantCulture), fact.FactId, fact.StockItemId, fact.LocationId));
            }
        }

        var ordered = items
            .OrderByDescending(x => x.OccurredAt)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.EventId)
            .ToArray();
        var safePage = Math.Max(1, request.Page);
        var safePageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageItems = ordered.Skip((int)Math.Min((long)(safePage - 1) * safePageSize, int.MaxValue)).Take(safePageSize).ToArray();
        return Result.Success(new OfferHistoryPage(pageItems, ordered.Length, safePage, safePageSize));
    }
}
