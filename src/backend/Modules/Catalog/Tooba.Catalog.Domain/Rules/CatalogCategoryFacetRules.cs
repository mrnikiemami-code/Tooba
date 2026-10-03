using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// اعتبارسنجی نوع نمایش facet بر اساس ValueKind.
/// </summary>
public static class CatalogCategoryFacetRules
{
    /// <summary>
    /// پیشنهاد نوع نمایش بر اساس ValueKind.
    /// </summary>
    public static CatalogFacetDisplayType SuggestDisplayType(CatalogAttributeValueKind valueKind) =>
        valueKind switch
        {
            CatalogAttributeValueKind.Boolean => CatalogFacetDisplayType.BooleanToggle,
            CatalogAttributeValueKind.Number => CatalogFacetDisplayType.Range,
            CatalogAttributeValueKind.Enumeration => CatalogFacetDisplayType.CheckboxList,
            CatalogAttributeValueKind.Text => CatalogFacetDisplayType.SearchableSelect,
            _ => CatalogFacetDisplayType.CheckboxList,
        };

    /// <summary>
    /// اعتبارسنجی ترکیب ValueKind و DisplayType؛ نتیجه typed است (نه exception/message).
    /// </summary>
    public static CatalogFacetDisplayTypeViolation ValidateDisplayType(
        CatalogAttributeDefinition definition,
        CatalogFacetDisplayType displayType)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (displayType == CatalogFacetDisplayType.ColorSwatch)
        {
            return CatalogFacetDisplayTypeViolation.ColorSwatchUnsupported;
        }

        switch (definition.ValueKind)
        {
            case CatalogAttributeValueKind.Boolean:
                if (displayType != CatalogFacetDisplayType.BooleanToggle)
                {
                    return CatalogFacetDisplayTypeViolation.BooleanRequiresToggle;
                }

                break;
            case CatalogAttributeValueKind.Number:
                if (displayType != CatalogFacetDisplayType.Range)
                {
                    return CatalogFacetDisplayTypeViolation.NumberRequiresRange;
                }

                break;
            case CatalogAttributeValueKind.Text:
                if (displayType is CatalogFacetDisplayType.Range or CatalogFacetDisplayType.BooleanToggle or CatalogFacetDisplayType.ColorSwatch)
                {
                    return CatalogFacetDisplayTypeViolation.TextDisplayIncompatible;
                }

                break;
            case CatalogAttributeValueKind.Enumeration:
                if (displayType is CatalogFacetDisplayType.Range or CatalogFacetDisplayType.BooleanToggle)
                {
                    return CatalogFacetDisplayTypeViolation.EnumerationDisplayIncompatible;
                }

                break;
            case CatalogAttributeValueKind.Instant:
                return CatalogFacetDisplayTypeViolation.InstantUnsupported;
        }

        if (displayType == CatalogFacetDisplayType.BooleanToggle && definition.ValueKind != CatalogAttributeValueKind.Boolean)
        {
            return CatalogFacetDisplayTypeViolation.BooleanToggleRequiresBoolean;
        }

        return CatalogFacetDisplayTypeViolation.None;
    }

    /// <summary>
    /// آیا IsSearchable برای این DisplayType مجاز است.
    /// </summary>
    public static bool IsSearchableAllowed(CatalogFacetDisplayType displayType) =>
        displayType is CatalogFacetDisplayType.CheckboxList or CatalogFacetDisplayType.SearchableSelect;
}
