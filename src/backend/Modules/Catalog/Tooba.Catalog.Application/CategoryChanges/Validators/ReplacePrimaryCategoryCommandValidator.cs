using FluentValidation;
using Tooba.Catalog.Application.CategoryChanges.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.CategoryChanges.Validators;

/// <summary>Transport shape for ReplacePrimaryCategoryCommand.</summary>
public sealed class ReplacePrimaryCategoryCommandValidator : AbstractValidator<ReplacePrimaryCategoryCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReplacePrimaryCategoryCommandValidator()
    {
        RuleFor(x => x.Model.NewCategoryId)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.CategoryChangeNewCategoryIdRequired);
    }
}
