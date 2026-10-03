using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.BulkInquiry.Contracts.Errors;

/// <summary>کاتالوگ کدهای خطای BulkInquiry.</summary>
public sealed class BulkInquiryErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(BulkInquiryErrorCodes.Rejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Bulk inquiry was rejected."),
        D(BulkInquiryErrorCodes.RequestRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Request is required."),
        D(BulkInquiryErrorCodes.SlugRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Slug is required."),
        D(BulkInquiryErrorCodes.FullNameRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Full name is required."),
        D(BulkInquiryErrorCodes.PhoneRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Phone is required."),
        D(BulkInquiryErrorCodes.AddressRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Address is required."),
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
