using FluentValidation;
using Tooba.Catalog.Application.Units.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Units.Validators;

/// <summary>Transport shape for UpdateUnitOfMeasureCommand.</summary>
public sealed class UpdateUnitOfMeasureCommandValidator : AbstractValidator<UpdateUnitOfMeasureCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateUnitOfMeasureCommandValidator()
    {
        RuleFor(x => x.UnitId)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.UnitIdRequired);
        RuleFor(x => x.Model.Code)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.UnitCodeRequired);
        RuleFor(x => x.Model.Dimension)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.UnitDimensionRequired);
        RuleFor(x => x.Model.Translations)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.UnitTranslationsRequired);
        RuleForEach(x => x.Model.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(CatalogValidationCodes.UnitTranslationNameRequired);
            t.RuleFor(x => x.ShortName)
                .NotEmpty()
                .WithErrorCode(CatalogValidationCodes.UnitTranslationShortNameRequired);
        });
    }
}
