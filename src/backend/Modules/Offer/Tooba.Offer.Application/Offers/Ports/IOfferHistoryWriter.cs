using Tooba.Offer.Domain.Aggregates;

namespace Tooba.Offer.Application.Offers.Ports;

/// <summary>
/// Stages one Offer-owned history entry on the current Offer unit of work. The owning command persists it
/// with its business write through the same Offer <see cref="IOfferStore.SaveChangesAsync"/> call so the
/// history and the Offer change commit together.
/// </summary>
public interface IOfferHistoryWriter
{
    /// <summary>Stages a history entry without committing it.</summary>
    Task AppendAsync(OfferHistoryEntry entry, CancellationToken cancellationToken);
}
