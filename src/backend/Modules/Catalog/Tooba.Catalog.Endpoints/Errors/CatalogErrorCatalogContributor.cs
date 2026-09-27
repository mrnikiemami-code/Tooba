using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Catalog.Contracts.Errors;

namespace Tooba.Catalog.Endpoints.Errors;

/// <summary>Canonical Catalog error catalog contributor.</summary>
public sealed class CatalogErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(CatalogErrorCodes.QuantityRoundingInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Quantity rounding mode is invalid."),
        D(CatalogErrorCodes.UnitMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Unit of measure was not found."),
        D(CatalogErrorCodes.UnitDimensionInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Unit dimension is invalid."),
        D(CatalogErrorCodes.UnitCodeDuplicate, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Unit code already exists."),
        D(CatalogErrorCodes.UnitLanguageUnknown, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Unit translation language is unknown."),
        D(CatalogErrorCodes.TagInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Tag input is invalid."),
        D(CatalogErrorCodes.TagCodeDuplicate, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Tag code already exists."),
        D(CatalogErrorCodes.TagMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Tag was not found."),
        D(CatalogErrorCodes.TagAssignDuplicate, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Tag is already assigned."),
        D(CatalogErrorCodes.TagProductMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Product was not found for tag assignment."),
        D(CatalogErrorCodes.TagCategoryMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Category was not found for tag assignment."),
        D(CatalogErrorCodes.MegaMenuCategoryMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Category was not found for MegaMenu."),
        D(CatalogErrorCodes.MegaMenuPlacementInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "MegaMenu placement is invalid."),
        D(CatalogErrorCodes.MegaMenuRemoveHasChildren, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "MegaMenu item still has presentation children."),
        D(CatalogErrorCodes.FacetCategoryMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Category was not found for Facet."),
        D(CatalogErrorCodes.FacetDefinitionMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Attribute definition was not found for Facet."),
        D(CatalogErrorCodes.FacetSchemaMissing, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Attribute is not in the category effective schema."),
        D(CatalogErrorCodes.FacetNotFilterable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Only filterable attributes may be configured as facets."),
        D(CatalogErrorCodes.FacetDisplayTypeInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Facet display type is invalid for the attribute value kind."),
        D(CatalogErrorCodes.FacetOverrideMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Local facet override was not found."),
        D(CatalogErrorCodes.FacetReorderInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Facet reorder list must match the local configuration set exactly."),
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
