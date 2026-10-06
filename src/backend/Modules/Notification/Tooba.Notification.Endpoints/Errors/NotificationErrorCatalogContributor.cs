using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Notification.Contracts.Errors;

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
        new(
            Code: NotificationErrorCodes.TargetRouteEmpty,
            Classification: ErrorClassification.Business,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: NotificationErrorCodes.TargetRouteEmpty,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Notification target route is required."),
        new(
            Code: NotificationErrorCodes.TargetRouteUnsafe,
            Classification: ErrorClassification.Business,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: NotificationErrorCodes.TargetRouteUnsafe,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Notification target route is not safe."),
        new(
            Code: NotificationErrorCodes.TargetRouteNotAllowed,
            Classification: ErrorClassification.Business,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: NotificationErrorCodes.TargetRouteNotAllowed,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Notification target route is not allowed."),
        new(
            Code: NotificationErrorCodes.RecipientKindInvalid,
            Classification: ErrorClassification.Business,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: NotificationErrorCodes.RecipientKindInvalid,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Notification recipient kind is invalid."),
        // customer.session.required is a shared cross-cutting code owned by
        // FoundationErrorCatalogContributor; Notification consumes it without re-registering.
    ];
}
