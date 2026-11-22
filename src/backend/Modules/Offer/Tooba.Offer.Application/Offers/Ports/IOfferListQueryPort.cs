using Tooba.Offer.Domain.Aggregates;

#pragma warning disable CS1591
namespace Tooba.Offer.Application.Offers.Ports;

/// <summary>Server-owned bounded filter for the admin/seller offer lists.</summary>
public sealed record OfferListCriteria(
    Guid? SellerPartyId,
    string? Search,
    string? Status,
    int Page,
    int PageSize,
    string? SortField,
    bool SortDescending);

/// <summary>Server page of Offer aggregates.</summary>
public sealed record OfferListPage(
    IReadOnlyList<SellerOffer> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>Bounded Offer list query port; no caller may materialize the whole seller list.</summary>
public interface IOfferListQueryPort
{
    Task<OfferListPage> QueryAsync(OfferListCriteria criteria, CancellationToken cancellationToken);
}

