namespace Tooba.Offer.Domain.Aggregates;

/// <summary>
/// Offer-owned link row binding one offer to one stable Shipping method identity.
/// </summary>
/// <remarks>
/// <para>
/// Only the canonical stable method identity is stored. No method code/label/rate/type copy and no
/// cross-module database foreign key: Shipping remains the owner of the method and its publication.
/// </para>
/// <para>
/// The row exists only while the offer's selection mode is <c>Specific</c>; switching to Automatic removes
/// every row. Uniqueness of <see cref="OfferId"/> + <see cref="MethodId"/> is enforced by the database.
/// </para>
/// </remarks>
public sealed class OfferShippingMethod
{
    /// <summary>Owning offer identity.</summary>
    public Guid OfferId { get; init; }

    /// <summary>Canonical stable method identity resolved from the owning Shipping contract.</summary>
    public Guid MethodId { get; init; }
}
