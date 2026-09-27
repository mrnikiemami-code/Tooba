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
}
