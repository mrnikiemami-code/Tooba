using FluentValidation;
using Tooba.Catalog.Application.Units.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Units.Validators;

/// <summary>Transport shape for CreateUnitOfMeasureCommand.</summary>
public sealed class CreateUnitOfMeasureCommandValidator : AbstractValidator<CreateUnitOfMeasureCommand>
{
    /// <summary>Creates the validator.</summary>
    public CreateUnitOfMeasureCommandValidator()
    {
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
