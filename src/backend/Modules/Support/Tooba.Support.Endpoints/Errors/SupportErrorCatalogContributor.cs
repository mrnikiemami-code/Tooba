using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Support.Application.Errors;

namespace Tooba.Support.Endpoints.Errors;

/// <summary>Explicit Support error catalog for ApiResponseFactory.</summary>
public sealed class SupportErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        // customer.session.required / seller.authorization.denied / admin.authorization.denied are
        // shared cross-cutting codes owned by FoundationErrorCatalogContributor.
        D(SupportErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Not Found"),
        D(SupportErrorCodes.Rejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(SupportErrorCodes.ReplyRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(SupportErrorCodes.ActionRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(SupportErrorCodes.PatchRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(SupportErrorCodes.DemoNotReady, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable, "Support demo seed not ready"),
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
