using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.PageComposition.Contracts.Errors;

namespace Tooba.PageComposition.Endpoints.Errors;

/// <summary>PageComposition error catalog contributor.</summary>
public sealed class PageCompositionErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PageCompositionErrorCodes.TenantMissing, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Page composition tenant was missing."),
        D(PageCompositionErrorCodes.SectionMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Page composition section was not found."),
        D(PageCompositionErrorCodes.SectionTypeRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Page composition section type was rejected."),
        D(PageCompositionErrorCodes.ConfigRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Page composition configuration was rejected."),
        D(PageCompositionErrorCodes.MutationRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Page composition mutation was rejected."),
    ];

    private static ErrorDescriptor D(string code, ErrorClassification classification, int status, string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
