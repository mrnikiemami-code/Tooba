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
