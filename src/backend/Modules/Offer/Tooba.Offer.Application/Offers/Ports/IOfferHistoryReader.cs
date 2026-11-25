using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Ports;

/// <summary>Bounded read port over the recorded Offer history.</summary>
public interface IOfferHistoryReader
{
    /// <summary>Reads one page ordered by occurrence descending; page and page size are clamped 1..100.</summary>
    Task<OfferHistoryPage> GetPageAsync(
        Guid offerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
