using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Pricing.Contracts;

namespace Tooba.Pricing.Endpoints.Errors;

/// <summary>Explicit Pricing error catalog for HTTP-reachable seller and campaign price writes.</summary>
public sealed class PricingErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PricingErrorCodes.AmountInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "The price amount is invalid."),
        D(PricingErrorCodes.Overlap, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "More than one active price overlaps this selection."),
        D(PricingErrorCodes.OfferMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Offer was not found."),
        D(PricingErrorCodes.CampaignRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "A campaign price requires a campaign."),
        D(PricingErrorCodes.ValidityInverted, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "The price validity window is invalid."),
        D(PricingErrorCodes.RetiredReactivate, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "A retired price cannot be activated again."),
        D(PricingErrorCodes.RetiredImmutable, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "A retired price cannot be changed."),
        D(PricingErrorCodes.CurrencyChangeForbidden, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Changing currency requires a new price."),
        D(PricingErrorCodes.MarketInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "The market code is invalid."),
        D(PricingErrorCodes.CurrencyInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "The currency code is invalid."),
        D(PricingErrorCodes.CurrencyDisplayUnit, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Use the stored currency code, not a display unit."),
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
