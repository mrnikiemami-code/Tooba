using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.ProductQnA.Contracts.Errors;

/// <summary>
/// کاتالوگ کدهای خطای ProductQnA.
/// تنها کدهای معنایی دامنه/کاربرد اینجا ثبت می‌شوند؛ کدهای اعتبارسنجی حمل‌ونقل
/// (<c>ProductQnAValidationCodes</c>) از طریق descriptor پایهٔ <c>validation.failed</c> نگاشت
/// می‌شوند و <c>customer.session.required</c> مالکیت Foundation دارد.
/// </summary>
public sealed class ProductQnAErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(ProductQnAErrorCodes.Rejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Product question was rejected."),
        D(ProductQnAErrorCodes.NotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Not Found"),
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
