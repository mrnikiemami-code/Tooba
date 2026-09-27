using FluentValidation;
using Tooba.Catalog.Application.Categories.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Categories.Validators;

/// <summary>Transport shape for UpsertCategoryTranslationCommand.</summary>
public sealed class UpsertCategoryTranslationCommandValidator : AbstractValidator<UpsertCategoryTranslationCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpsertCategoryTranslationCommandValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n))
            .WithErrorCode(CatalogValidationCodes.CategoryTranslationNameRequired);
        RuleFor(x => x.Slug)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithErrorCode(CatalogValidationCodes.CategoryTranslationSlugRequired);
    }
}
