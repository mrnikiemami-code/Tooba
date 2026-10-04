using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Cart.Contracts.Errors;

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
        // A quoted line must always carry its own currency truth; when it is absent the fail-closed
        // outcome is the line-level equivalent of a missing quote, so it follows the same
        // Business/409 convention as cart.pricing.quote_missing rather than the unexpected 500 fallback.
        D(CartErrorCodes.LineCurrencyMissing, ErrorClassification.Business, StatusCodes.Status409Conflict,
            "Cart line currency is unavailable. Please retry."),
        D(CartErrorCodes.OfferUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "This offer cannot be added to the cart."),
        D(CartErrorCodes.InventoryInsufficient, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Selected quantity exceeds sellable inventory."),
        D(CartErrorCodes.InventoryStale, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Inventory changed. Review the quantity and retry."),
        D(CartErrorCodes.Rejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cart operation was rejected. Please retry."),
        D(CartErrorCodes.UserIdRequired, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "A signed-in customer is required."),
        D(CartErrorCodes.GuestCredentialHashRequired, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "Guest cart credential is missing."),
        D(CartErrorCodes.LineMergeViaQuantity, ErrorClassification.Business, StatusCodes.Status409Conflict,
            "This offer already exists in the cart."),
        D(CartErrorCodes.ConversionOrderRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cart conversion requires a concrete order path."),
        D(CartErrorCodes.AdoptGuestOnly, ErrorClassification.Business, StatusCodes.Status409Conflict,
            "Only a guest cart can be attached to a signed-in customer."),
        D(CartErrorCodes.ExpiryFutureRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cart expiry must be in the future."),
        D(CartErrorCodes.ExpiryAfterCreated, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cart expiry must be after its creation time."),
        D(CartErrorCodes.MarketRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cart market is not configured."),
        D(CartErrorCodes.CurrencyInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cart currency selection is not valid."),
        D(CartErrorCodes.PricingQuoteMissing, ErrorClassification.Business, StatusCodes.Status409Conflict,
            "No price is available for this offer."),
        D(CartErrorCodes.CommerceContextUnavailable, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable,
            "Store commerce context is unavailable."),
        D(CartErrorCodes.CommerceMarketUnconfigured, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable,
            "Store commerce market is not configured."),
        D(CartErrorCodes.CommerceCurrencyUnconfigured, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable,
            "Store commerce currency is not configured."),
        D(CartErrorCodes.CommerceChannelUnconfigured, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable,
            "Store commerce sales channel is not configured."),
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
