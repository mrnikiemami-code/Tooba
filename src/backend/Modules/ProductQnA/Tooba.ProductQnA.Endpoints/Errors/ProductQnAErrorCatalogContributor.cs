using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.ProductQnA.Contracts.Errors;

namespace Tooba.ProductQnA.Endpoints.Errors;

/// <summary>کاتالوگ کدهای خطای ProductQnA.</summary>
public sealed class ProductQnAErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(ProductQnAErrorCodes.Rejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Product question was rejected."),
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
