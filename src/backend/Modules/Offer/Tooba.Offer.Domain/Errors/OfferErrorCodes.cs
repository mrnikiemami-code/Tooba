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
    /// <summary>An archived offer cannot take a fulfillment assignment.</summary>
    public const string ArchivedCannotAssignFulfillment = "offer.archived.cannot_assign_fulfillment";
    /// <summary>The requested fulfillment responsibility is not one of the two supported families.</summary>
    public const string FulfillmentResponsibilityUnsupported = "offer.fulfillment.responsibility_unsupported";
    /// <summary>A fulfillment shipping type id cannot be stored without an explicit responsibility.</summary>
    public const string FulfillmentTypeWithoutResponsibility = "offer.fulfillment.type_without_responsibility";
    /// <summary>The fulfillment shipping type identity is structurally invalid.</summary>
    public const string FulfillmentTypeInvalid = "offer.fulfillment.type_invalid";
}
