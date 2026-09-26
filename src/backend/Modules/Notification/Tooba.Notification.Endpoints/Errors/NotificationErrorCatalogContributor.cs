using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Notification.Application.Errors;

namespace Tooba.Notification.Endpoints.Errors;

/// <summary>Explicit Notification error catalog for HTTP outcomes.</summary>
public sealed class NotificationErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        new(
            Code: NotificationErrorCodes.Missing,
            Classification: ErrorClassification.NotFound,
            HttpStatus: StatusCodes.Status404NotFound,
            LocalizationKey: NotificationErrorCodes.Missing,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Notification was not found."),
        // customer.session.required is a shared cross-cutting code owned by
        // FoundationErrorCatalogContributor; Notification consumes it without re-registering.
    ];
}
