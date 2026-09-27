namespace Tooba.Catalog.Application.Validators;

/// <summary>Stable Catalog transport validation codes.</summary>
public static class CatalogValidationCodes
{
    /// <summary>Global rounding mode must be non-blank on PUT.</summary>
    public const string QuantityRoundingModeRequired = "catalog.validation.quantity_rounding_mode_required";
}
