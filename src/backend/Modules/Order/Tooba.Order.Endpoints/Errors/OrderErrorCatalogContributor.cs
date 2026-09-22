using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Application.Admin.Completeness.Errors;

namespace Tooba.Order.Endpoints.Errors;

/// <summary>کاتالوگ صریح کدهای خطای Order برای مسیرهای API مدیریت سفارش.</summary>
public sealed class OrderErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(AdminOrderCompletenessErrors.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Order was not found."),
        D(AdminOrderCompletenessErrors.InvalidNote, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Internal note body is required."),
        D(AdminOrderCompletenessErrors.DeleteForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden,
            "This internal note can no longer be deleted."),
        D(AdminOrderCompletenessErrors.InvoiceUnavailable, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Invoice is not available for this order."),
        D(AdminOrderCompletenessErrors.ReceiptUnavailable, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Payment receipt is not available for this order."),
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
