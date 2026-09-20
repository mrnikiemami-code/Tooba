namespace Tooba.Offer.Domain.Errors;

/// <summary>Stable semantic error codes owned by the Offer domain.</summary>
public static class OfferErrorCodes
{
    /// <summary>The minimum order quantity is invalid.</summary>
    public const string MinQuantityInvalid = "offer.min_quantity.invalid";
    /// <summary>The maximum order quantity is invalid.</summary>
    public const string MaxQuantityInvalid = "offer.max_quantity.invalid";
    /// <summary>The minimum order quantity exceeds the maximum.</summary>
    public const string MinQuantityExceedsMax = "offer.min_quantity.exceeds_max";
    /// <summary>An archived offer cannot be activated.</summary>
    public const string ArchivedCannotActivate = "offer.archived.cannot_activate";
}
