using FluentValidation;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Settings;

/// <summary>Transport shape for SaveStoreQuantitySettingsCommand — mode required non-blank.</summary>
public sealed class SaveStoreQuantitySettingsCommandValidator : AbstractValidator<SaveStoreQuantitySettingsCommand>
{
    /// <summary>Creates the validator.</summary>
    public SaveStoreQuantitySettingsCommandValidator()
    {
        RuleFor(x => x.GlobalRoundingMode)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.QuantityRoundingModeRequired);
    }
}
