namespace Tooba.Offer.Contracts.Dtos;

/// <summary>
/// Public commercial offer status.
/// </summary>
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
