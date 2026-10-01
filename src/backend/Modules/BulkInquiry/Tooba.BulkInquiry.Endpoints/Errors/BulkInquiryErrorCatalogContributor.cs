using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BulkInquiry.Contracts.Errors;

namespace Tooba.BulkInquiry.Endpoints.Errors;

/// <summary>کاتالوگ کدهای خطای BulkInquiry.</summary>
public sealed class BulkInquiryErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(BulkInquiryErrorCodes.Rejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Bulk inquiry was rejected."),
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
