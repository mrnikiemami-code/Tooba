using FluentValidation;
using Tooba.Catalog.Application.CategoryChanges.Queries;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.CategoryChanges.Validators;

/// <summary>Transport shape for PreviewCategoryChangeQuery.</summary>
public sealed class PreviewCategoryChangeQueryValidator : AbstractValidator<PreviewCategoryChangeQuery>
{
    /// <summary>Creates the validator.</summary>
    public PreviewCategoryChangeQueryValidator()
    {
        RuleFor(x => x.Model.NewCategoryId)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.CategoryChangeNewCategoryIdRequired);
    }
}
