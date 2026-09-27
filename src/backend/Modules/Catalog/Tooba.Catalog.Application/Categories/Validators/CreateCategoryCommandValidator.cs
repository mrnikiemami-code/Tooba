using FluentValidation;
using Tooba.Catalog.Application.Categories.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Categories.Validators;

/// <summary>Transport shape for CreateCategoryCommand.</summary>
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    /// <summary>Creates the validator.</summary>
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Model)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.CategoryCreateShapeRequired)
            .Must(m =>
                (m.Translations is { Count: > 0 })
                || (m.LocalizedNames is { Count: > 0 }
                    && m.LocalizedNames.Values.Any(v => !string.IsNullOrWhiteSpace(v))))
            .WithErrorCode(CatalogValidationCodes.CategoryCreateShapeRequired);
    }
}
