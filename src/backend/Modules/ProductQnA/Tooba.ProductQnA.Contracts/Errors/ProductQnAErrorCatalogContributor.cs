using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.ProductQnA.Contracts.Errors;

/// <summary>کاتالوگ کدهای خطای ProductQnA.</summary>
public sealed class ProductQnAErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(ProductQnAErrorCodes.Rejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Product question was rejected."),
        D(ProductQnAErrorCodes.NotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Not Found"),
        D(ProductQnAErrorCodes.ActorRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Actor is required."),
        D(ProductQnAErrorCodes.BodyRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Body is required."),
        D(ProductQnAErrorCodes.ProductRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Product is required."),
        D(ProductQnAErrorCodes.QuestionBodyRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Question body is required."),
        D(ProductQnAErrorCodes.SlugRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Slug is required."),
        D(ProductQnAErrorCodes.PageInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Page is invalid."),
        D(ProductQnAErrorCodes.PageSizeInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Page size is invalid."),
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
