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
        D(CatalogErrorCodes.CategoryMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Category was not found."),
        D(CatalogErrorCodes.CategoryInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Category input is invalid."),
        D(CatalogErrorCodes.CategorySlugInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Category slug is invalid."),
        D(CatalogErrorCodes.CategorySlugDuplicate, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Category slug is already in use for this locale."),
        D(CatalogErrorCodes.CategoryParentMissing, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Parent category was not found."),
        D(CatalogErrorCodes.CategorySelfParent, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Category cannot be its own parent."),
        D(CatalogErrorCodes.CategoryDescendantParent, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Category cannot be moved under its own descendant."),
        D(CatalogErrorCodes.CategoryMaxDepth, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Category hierarchy exceeds maximum depth."),
        D(CatalogErrorCodes.CategoryReorderInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Category reorder list must match the sibling set exactly."),
        D(CatalogErrorCodes.CategoryConcurrencyConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Category was modified concurrently."),
        D(CatalogErrorCodes.CategoryRouteInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Category route resolve input is invalid."),
        D(CatalogErrorCodes.CategoryRouteMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Category route was not found."),
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
