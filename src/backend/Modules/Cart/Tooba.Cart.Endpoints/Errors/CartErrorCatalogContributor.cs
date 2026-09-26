using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Cart.Application.Errors;

namespace Tooba.Cart.Endpoints.Errors;

/// <summary>Explicit Cart error catalog for storefront HTTP outcomes.</summary>
public sealed class CartErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(CartErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Cart was not found."),
        D(CartErrorCodes.GuestInvalid, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "Guest cart access is not valid."),
        D(CartErrorCodes.AccessDenied, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "Cart access denied."),
        D(CartErrorCodes.VersionConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Cart was updated concurrently. Refresh and retry."),
        D(CartErrorCodes.Expired, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Cart has expired."),
        D(CartErrorCodes.QuantityInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Selected quantity is not valid."),
        D(CartErrorCodes.LineMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Cart line was not found."),
        D(CartErrorCodes.OfferUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "This offer cannot be added to the cart."),
        D(CartErrorCodes.InventoryInsufficient, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Selected quantity exceeds sellable inventory."),
        D(CartErrorCodes.InventoryStale, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Inventory changed. Review the quantity and retry."),
        D(CartErrorCodes.Rejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cart operation was rejected. Please retry."),
        // checkout.authentication_required descriptor is owned by
        // FoundationErrorCatalogContributor; Cart consumes the shared code without re-registering.
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
