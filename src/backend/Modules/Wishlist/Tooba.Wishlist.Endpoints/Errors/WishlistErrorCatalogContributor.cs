using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Endpoints.Errors;

/// <summary>کاتالوگ صریح کدهای خطای Wishlist برای مرز HTTP مشتری.</summary>
public sealed class WishlistErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(WishlistErrorCodes.ProductUnavailable, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Published product was not found."),
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
