using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Tax.Contracts.Errors;

/// <summary>
/// Tax-owned error catalog. Registers one descriptor per Tax-emitted machine code so the canonical
/// <c>SafeErrorMapper</c> classifies them instead of falling back to a generic 400.
/// <para>
/// Tax is <c>INTERNAL_ONLY</c> (zero HTTP routes, no Endpoints project); the descriptors still pin the
/// semantics for the module's consumer seams (Order checkout calculation, Catalog storefront
/// composition, ProductWorkspace admin classification reads). Classification is by stable machine code
/// only.
/// </para>
/// <para>
/// <c>checkout.tax.unavailable</c> is deliberately absent: it is declared and owned by Order
/// (<c>StorefrontOrderErrors.CheckoutTaxUnavailable</c>), so Tax must not claim its descriptor.
/// </para>
/// </summary>
public sealed class TaxErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(TaxErrorCodes.RuleIdRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "A tax rule id is required."),
        D(TaxErrorCodes.JurisdictionRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "A tax jurisdiction is required."),
        D(TaxErrorCodes.MarketRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "A tax market is required."),
        D(TaxErrorCodes.ValidityInverted, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "The tax rule validity window is invalid."),
        D(TaxErrorCodes.RateOutOfRange, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "The tax rate is out of range."),
        D(TaxErrorCodes.RateNotApplicable, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "A non-percentage tax rule must not carry a rate."),
        D(TaxErrorCodes.RateKindMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Only a percentage tax rule carries an editable rate."),
        D(TaxErrorCodes.CategoryIdRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "A tax category id is required."),
        D(TaxErrorCodes.CategoryCodeRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "A tax category code is required."),
        D(TaxErrorCodes.CategoryMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "The tax category was not found."),
        D(TaxErrorCodes.OutboxUnmappedEventType, ErrorClassification.Platform, StatusCodes.Status500InternalServerError,
            "Integration event type is not registered."),
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
