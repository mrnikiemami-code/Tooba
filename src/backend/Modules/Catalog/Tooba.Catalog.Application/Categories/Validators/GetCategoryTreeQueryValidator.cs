using FluentValidation;
using Tooba.Catalog.Application.Categories.Queries;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Categories.Validators;

/// <summary>Transport shape for GetCategoryTreeQuery.</summary>
public sealed class GetCategoryTreeQueryValidator : AbstractValidator<GetCategoryTreeQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetCategoryTreeQueryValidator()
    {
        RuleFor(x => x.Locale)
            .Must(l => !string.IsNullOrWhiteSpace(l))
            .WithErrorCode(CatalogValidationCodes.CategoryLocaleRequired);
    }
}
