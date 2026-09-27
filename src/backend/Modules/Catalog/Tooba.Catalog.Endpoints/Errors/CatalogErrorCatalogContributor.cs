using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Catalog.Contracts.Errors;

namespace Tooba.Catalog.Endpoints.Errors;

/// <summary>Canonical Catalog error catalog contributor.</summary>
public sealed class CatalogErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(CatalogErrorCodes.QuantityRoundingInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Quantity rounding mode is invalid."),
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
