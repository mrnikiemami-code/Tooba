namespace Tooba.Catalog.Contracts.Errors;

/// <summary>Stable Catalog semantic error codes for HTTP presentation.</summary>
public static class CatalogErrorCodes
{
    /// <summary>Invalid global quantity rounding mode on admin settings write.</summary>
    public const string QuantityRoundingInvalid = "quantity.rounding.invalid";

    /// <summary>Requested unit of measure was not found.</summary>
    public const string UnitMissing = "unit.missing";

    /// <summary>Unit dimension string is not a known enum value.</summary>
    public const string UnitDimensionInvalid = "unit.dimension.invalid";

    /// <summary>Unit code already exists.</summary>
    public const string UnitCodeDuplicate = "unit.code.duplicate";

    /// <summary>Translation LanguageId is not in the language registry.</summary>
    public const string UnitLanguageUnknown = "unit.language.unknown";

    /// <summary>Tag create input is invalid (e.g. missing localized name).</summary>
    public const string TagInvalid = "catalog.tag.invalid";

    /// <summary>Explicit tag code already exists.</summary>
    public const string TagCodeDuplicate = "catalog.tag.code.duplicate";

    /// <summary>Requested tag was not found.</summary>
    public const string TagMissing = "catalog.tag.missing";

    /// <summary>Tag already assigned to product or category.</summary>
    public const string TagAssignDuplicate = "catalog.tag.assign.duplicate";

    /// <summary>Referenced product was not found for tag assignment.</summary>
    public const string TagProductMissing = "catalog.tag.product.missing";

    /// <summary>Referenced category was not found for tag assignment.</summary>
    public const string TagCategoryMissing = "catalog.tag.category.missing";

    /// <summary>Referenced category was not found for MegaMenu Admin operations.</summary>
    public const string MegaMenuCategoryMissing = "catalog.megamenu.category.missing";

    /// <summary>MegaMenu presentation tree placement rule violated.</summary>
    public const string MegaMenuPlacementInvalid = "catalog.megamenu.placement.invalid";

    /// <summary>Cannot remove a MegaMenu item that still has presentation children.</summary>
    public const string MegaMenuRemoveHasChildren = "catalog.megamenu.remove.has_children";

    /// <summary>Referenced category was not found for Facet operations.</summary>
    public const string FacetCategoryMissing = "catalog.facet.category.missing";

    /// <summary>Referenced attribute definition was not found for Facet operations.</summary>
    public const string FacetDefinitionMissing = "catalog.facet.definition.missing";

    /// <summary>Definition is not present in the category effective schema.</summary>
    public const string FacetSchemaMissing = "catalog.facet.schema.missing";

    /// <summary>Only filterable effective-schema attributes may be configured as facets.</summary>
    public const string FacetNotFilterable = "catalog.facet.not_filterable";

    /// <summary>Facet display type is incompatible with the attribute ValueKind.</summary>
    public const string FacetDisplayTypeInvalid = "catalog.facet.display_type.invalid";

    /// <summary>Local facet override row was not found for remove.</summary>
    public const string FacetOverrideMissing = "catalog.facet.override.missing";

    /// <summary>Reorder list is incomplete, duplicate, or mismatched vs local facet set.</summary>
    public const string FacetReorderInvalid = "catalog.facet.reorder.invalid";
}
