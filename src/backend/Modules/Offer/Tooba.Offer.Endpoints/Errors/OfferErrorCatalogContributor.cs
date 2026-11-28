using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Offer.Contracts.Errors;

namespace Tooba.Offer.Endpoints.Errors;

/// <summary>کاتالوگ صریح کدهای خطای Offer برای مسیرهای API فروشنده.</summary>
public sealed class OfferErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(OfferErrorCodes.PriceAmountRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Price amount is required."),
        D(OfferErrorCodes.MinQuantityInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Minimum order quantity must be greater than zero."),
        D(OfferErrorCodes.MaxQuantityInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Maximum order quantity must be greater than zero."),
        D(OfferErrorCodes.MinQuantityExceedsMax, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Minimum order quantity cannot exceed maximum."),
        D(OfferErrorCodes.ArchivedCannotActivate, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "An archived offer cannot be reactivated; create a new listing."),
        D(OfferErrorCodes.CatalogVariantMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Catalog variant was not found."),
        D(OfferErrorCodes.SellerMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Seller was not found."),
        D(OfferErrorCodes.SellerNotOrganization, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Seller must be an Organization party."),
        D(OfferErrorCodes.DuplicateActiveListing, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "An active offer already exists for this seller, variant, and channel."),
        D(OfferErrorCodes.DuplicateSellerSku, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Seller SKU is already used by this seller."),
        D(OfferErrorCodes.CreateIntentConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "The create request id was reused with a different seller, variant, SKU, status or commercial intent."),
        D(OfferErrorCodes.ReturnPolicyOverrideDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "Seller cannot override return policy."),
        D(OfferErrorCodes.NonReturnableDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "Non-returnable offers are not allowed."),
        D(OfferErrorCodes.CustomReturnWindowRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Custom return window days are required."),
        D(OfferErrorCodes.CustomReturnWindowOutOfRange, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Return window must be between {min} and {max} days."),
        D(OfferErrorCodes.NotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Offer was not found."),
        D(OfferErrorCodes.StatusUnsupported, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Requested offer status is unsupported."),
        D(OfferErrorCodes.ChannelRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "No offer channel is allowed for this edition."),
        D(OfferErrorCodes.ChannelNotAllowed, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "The requested offer channel is not allowed for this edition."),
        D(OfferErrorCodes.ArchivedCannotAssignFulfillment, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "An archived offer cannot take a fulfillment assignment."),
        D(OfferErrorCodes.FulfillmentResponsibilityUnsupported, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Requested fulfillment responsibility is unsupported."),
        D(OfferErrorCodes.FulfillmentTypeWithoutResponsibility, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "A fulfillment shipping type cannot be stored without an explicit responsibility."),
        D(OfferErrorCodes.FulfillmentTypeInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment shipping type identity is invalid."),
        D(OfferErrorCodes.FulfillmentTypeUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment shipping type is not available in the active catalog."),
        D(OfferErrorCodes.FulfillmentChangeProtected, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Fulfillment responsibility cannot change while existing commitments or allocation are protected."),
        D(OfferErrorCodes.FulfillmentBodyMalformed, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Fulfillment assignment body is malformed."),
        D(OfferErrorCodes.ShippingAssignmentRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Select a fulfillment responsibility and shipping type before choosing specific methods."),
        D(OfferErrorCodes.ShippingMethodsRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Select at least one shipping method."),
        D(OfferErrorCodes.ShippingMethodInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "The shipping method selection is invalid."),
        D(OfferErrorCodes.ShippingMethodDuplicate, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "A shipping method was selected more than once."),
        D(OfferErrorCodes.ShippingMethodUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "A selected shipping method is not published and active for this owner and type."),
        D(OfferErrorCodes.ShippingReconciliationRequired, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Changing the responsibility or shipping type requires explicitly reconciling the selected methods."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(
            Code: code,
            Classification: classification,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}

