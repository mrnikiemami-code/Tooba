namespace Tooba.Offer.Domain.ValueObjects;

/// <summary>Commercial lifecycle status owned by the Offer domain.</summary>
public enum OfferStatus
{
    /// <summary>Draft listing.</summary>
    Draft = 0,
    /// <summary>Active listing.</summary>
    Active = 1,
    /// <summary>Suspended listing.</summary>
    Suspended = 2,
    /// <summary>Archived listing.</summary>
    Archived = 3,
}
