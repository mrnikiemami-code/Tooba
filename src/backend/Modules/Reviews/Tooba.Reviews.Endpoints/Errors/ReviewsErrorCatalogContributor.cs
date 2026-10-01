using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Reviews.Contracts.Errors;

namespace Tooba.Reviews.Endpoints.Errors;

/// <summary>Reviews error catalog contributor.</summary>
public sealed class ReviewsErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(ReviewsErrorCodes.Duplicate, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Review already exists for this product."),
        D(ReviewsErrorCodes.Rejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Review submission was rejected."),
        D(ReviewsErrorCodes.ModerationRejected, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Review moderation was rejected."),
        // customer.session.required is owned by FoundationErrorCatalogContributor — do not re-register.
    ];

    private static ErrorDescriptor D(string code, ErrorClassification classification, int status, string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
