namespace Tooba.Catalog.Application.Validators;

/// <summary>Stable Catalog transport validation codes.</summary>
public static class CatalogValidationCodes
{
    /// <summary>Global rounding mode must be non-blank on PUT.</summary>
    public const string QuantityRoundingModeRequired = "catalog.validation.quantity_rounding_mode_required";

    /// <summary>Unit code must be non-blank.</summary>
    public const string UnitCodeRequired = "catalog.validation.unit_code_required";

    /// <summary>Unit dimension must be non-blank.</summary>
    public const string UnitDimensionRequired = "catalog.validation.unit_dimension_required";

    /// <summary>Translations collection must be present.</summary>
    public const string UnitTranslationsRequired = "catalog.validation.unit_translations_required";

    /// <summary>Translation name must be non-blank.</summary>
    public const string UnitTranslationNameRequired = "catalog.validation.unit_translation_name_required";

    /// <summary>Translation short name must be non-blank.</summary>
    public const string UnitTranslationShortNameRequired = "catalog.validation.unit_translation_short_name_required";

    /// <summary>Unit id must be non-empty on update.</summary>
    public const string UnitIdRequired = "catalog.validation.unit_id_required";

    /// <summary>Tag code exceeds max length.</summary>
    public const string TagCodeTooLong = "catalog.validation.tag_code_too_long";

    /// <summary>Tag slug exceeds max length.</summary>
    public const string TagSlugTooLong = "catalog.validation.tag_slug_too_long";

    /// <summary>LocalizedNames dictionary must be present (may be empty before overlays).</summary>
    public const string TagLocalizedNamesRequired = "catalog.validation.tag_localized_names_required";

    /// <summary>LocalizedNames keys must be non-blank.</summary>
    public const string TagLocalizedNameLocaleRequired = "catalog.validation.tag_localized_name_locale_required";

    /// <summary>MegaMenu binding body must be present on PUT.</summary>
    public const string MegaMenuBindingInputRequired = "catalog.validation.megamenu_binding_input_required";

    /// <summary>MegaMenu title override exceeds max length.</summary>
    public const string MegaMenuTitleOverrideTooLong = "catalog.validation.megamenu_title_override_too_long";

    /// <summary>MegaMenu badge text exceeds max length.</summary>
    public const string MegaMenuBadgeTextTooLong = "catalog.validation.megamenu_badge_text_too_long";

    /// <summary>MegaMenu short label exceeds max length.</summary>
    public const string MegaMenuShortLabelTooLong = "catalog.validation.megamenu_short_label_too_long";

    /// <summary>Facet upsert body must be present on PUT.</summary>
    public const string FacetInputRequired = "catalog.validation.facet_input_required";

    /// <summary>Facet reorder orderedDefinitionIds collection must be present.</summary>
    public const string FacetOrderedDefinitionIdsRequired = "catalog.validation.facet_ordered_definition_ids_required";

    /// <summary>Category tree locale query must be non-blank.</summary>
    public const string CategoryLocaleRequired = "catalog.validation.category_locale_required";

    /// <summary>Category create must supply Translations or LocalizedNames.</summary>
    public const string CategoryCreateShapeRequired = "catalog.validation.category_create_shape_required";

    /// <summary>Category translation name must be non-blank.</summary>
    public const string CategoryTranslationNameRequired = "catalog.validation.category_translation_name_required";

    /// <summary>Category translation slug must be non-blank.</summary>
    public const string CategoryTranslationSlugRequired = "catalog.validation.category_translation_slug_required";

    /// <summary>Category reorder orderedCategoryIds collection must be present.</summary>
    public const string CategoryOrderedIdsRequired = "catalog.validation.category_ordered_ids_required";

    /// <summary>Category route resolve locale must be non-blank.</summary>
    public const string CategoryRouteLocaleRequired = "catalog.validation.category_route_locale_required";

    /// <summary>Category route resolve slug must be non-blank.</summary>
    public const string CategoryRouteSlugRequired = "catalog.validation.category_route_slug_required";
}
