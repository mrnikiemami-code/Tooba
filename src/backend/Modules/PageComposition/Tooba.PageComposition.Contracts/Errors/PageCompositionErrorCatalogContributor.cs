using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.PageComposition.Contracts.Errors;

/// <summary>کاتالوگ کدهای خطای PageComposition.</summary>
public sealed class PageCompositionErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PageCompositionErrorCodes.TenantMissing, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Page composition tenant was missing."),
        D(PageCompositionErrorCodes.SectionMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Page composition section was not found."),
        D(PageCompositionErrorCodes.SectionTypeRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Page composition section type was rejected."),
        D(PageCompositionErrorCodes.ConfigRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Page composition configuration was rejected."),
        D(PageCompositionErrorCodes.MutationRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Page composition mutation was rejected."),
        D(PageCompositionErrorCodes.SectionTypeRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Section type is required."),
        D(PageCompositionErrorCodes.SectionIdsRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Section ids are required."),
        D(PageCompositionErrorCodes.SectionIdRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Section id is required."),
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
