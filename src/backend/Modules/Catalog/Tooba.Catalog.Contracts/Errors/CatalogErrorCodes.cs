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

    /// <summary>Requested category was not found.</summary>
    public const string CategoryMissing = "catalog.category.missing";

    /// <summary>Category input or business precondition is invalid.</summary>
    public const string CategoryInvalid = "catalog.category.invalid";

    /// <summary>Category slug is empty or invalid after normalization.</summary>
    public const string CategorySlugInvalid = "catalog.category.slug.invalid";

    /// <summary>Category slug already used by another category for the locale.</summary>
    public const string CategorySlugDuplicate = "catalog.category.slug.duplicate";

    /// <summary>Referenced parent category was not found.</summary>
    public const string CategoryParentMissing = "catalog.category.parent.missing";

    /// <summary>Category cannot be its own parent.</summary>
    public const string CategorySelfParent = "catalog.category.parent.self";

    /// <summary>Category cannot be moved under its own descendant.</summary>
    public const string CategoryDescendantParent = "catalog.category.parent.descendant";

    /// <summary>Category hierarchy would exceed max depth of 3.</summary>
    public const string CategoryMaxDepth = "catalog.category.depth.max";

    /// <summary>Reorder list must match the exact sibling set under the parent.</summary>
    public const string CategoryReorderInvalid = "catalog.category.reorder.invalid";

    /// <summary>ExpectedUpdatedAt concurrency token does not match.</summary>
    public const string CategoryConcurrencyConflict = "catalog.category.concurrency.conflict";

    /// <summary>Storefront/admin category route resolve input is invalid.</summary>
    public const string CategoryRouteInvalid = "catalog.category.route.invalid";

    /// <summary>Category route locale+slug could not be resolved.</summary>
    public const string CategoryRouteMissing = "catalog.category.route.missing";

    /// <summary>Requested attribute definition was not found.</summary>
    public const string AttributeMissing = "catalog.attribute.missing";

    /// <summary>Attribute definition input or business precondition is invalid.</summary>
    public const string AttributeInvalid = "catalog.attribute.invalid";

    /// <summary>Attribute definition code already exists.</summary>
    public const string AttributeCodeDuplicate = "catalog.attribute.code.duplicate";

    /// <summary>Localized attribute definition name already exists for the locale.</summary>
    public const string AttributeNameDuplicate = "catalog.attribute.name.duplicate";

    /// <summary>ValueKind cannot be used as a variant axis.</summary>
    public const string AttributeVariantAxisValueKindInvalid = "catalog.attribute.variant_axis.value_kind.invalid";

    /// <summary>Variant-axis capability cannot be disabled while in use.</summary>
    public const string AttributeVariantAxisInUse = "catalog.attribute.variant_axis.in_use";

    /// <summary>Variant-axis assignment blocked because definition capability is disabled.</summary>
    public const string AttributeVariantAxisCapabilityDisabled = "catalog.attribute.variant_axis.capability_disabled";

    /// <summary>Category was not found for attribute-schema operations.</summary>
    public const string SchemaCategoryMissing = "catalog.schema.category.missing";

    /// <summary>Category attribute binding already exists.</summary>
    public const string SchemaBindingDuplicate = "catalog.schema.binding.duplicate";

    /// <summary>Category attribute binding was not found.</summary>
    public const string SchemaBindingMissing = "catalog.schema.binding.missing";

    /// <summary>Reorder list is incomplete, duplicate, or mismatched vs local binding set.</summary>
    public const string SchemaReorderInvalid = "catalog.schema.reorder.invalid";

    /// <summary>Generic category attribute-schema precondition failure.</summary>
    public const string SchemaInvalid = "catalog.schema.invalid";
}
