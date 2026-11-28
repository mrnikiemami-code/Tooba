namespace Tooba.Offer.Application.Offers.Ports;

/// <summary>
/// Offer-owned link-row persistence for an explicit shipping method selection.
/// </summary>
/// <remarks>
/// All methods stage work on the current Offer unit of work; the owning command persists it together with the
/// Offer mode and history through the same <see cref="IOfferStore.SaveChangesAsync"/> call.
/// </remarks>
public interface IOfferShippingMethodStore
{
    /// <summary>Current canonical method identities of one offer, ordered deterministically.</summary>
    /// <param name="offerId">Owning offer identity.</param>
    /// <param name="cancellationToken">Caller token.</param>
    Task<IReadOnlyList<Guid>> ListMethodIdsAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>
    /// Stages replacement of the offer's entire link set with exactly the supplied identities.
    /// </summary>
    /// <param name="offerId">Owning offer identity.</param>
    /// <param name="methodIds">Deterministic distinct target identities; empty clears every row.</param>
    /// <param name="cancellationToken">Caller token.</param>
    Task ReplaceMethodIdsAsync(Guid offerId, IReadOnlyList<Guid> methodIds, CancellationToken cancellationToken);
}
