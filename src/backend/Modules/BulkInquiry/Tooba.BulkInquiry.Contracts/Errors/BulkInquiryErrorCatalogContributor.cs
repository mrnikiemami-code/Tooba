using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.BulkInquiry.Contracts.Errors;

/// <summary>
/// کاتالوگ کدهای خطای BulkInquiry.
/// فقط کدهای معنایی دامنه/کاربرد ثبت می‌شوند؛ کدهای اعتبارسنجی حمل‌ونقل
/// (<c>BulkInquiryValidationCodes</c>) از طریق descriptor پایهٔ <c>validation.failed</c> نگاشت می‌شوند.
/// </summary>
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
