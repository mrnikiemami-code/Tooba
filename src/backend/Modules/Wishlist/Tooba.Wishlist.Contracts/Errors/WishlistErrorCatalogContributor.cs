using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Wishlist.Contracts.Errors;

/// <summary>کاتالوگ صریح کدهای خطای Wishlist برای مرز HTTP مشتری.</summary>
public sealed class WishlistErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(WishlistErrorCodes.ProductUnavailable, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Published product was not found."),
        D(WishlistErrorCodes.ProductIdRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Product id is required."),
        D(WishlistErrorCodes.ProductIdsRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Product ids are required."),
        // customer.session.required is owned by FoundationErrorCatalogContributor.
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
