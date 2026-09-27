using FluentValidation;
using Tooba.Catalog.Application.Categories.Queries;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Categories.Validators;

/// <summary>Transport shape for ResolveCategoryRouteQuery.</summary>
public sealed class ResolveCategoryRouteQueryValidator : AbstractValidator<ResolveCategoryRouteQuery>
{
    /// <summary>Creates the validator.</summary>
    public ResolveCategoryRouteQueryValidator()
    {
        RuleFor(x => x.Locale)
            .Must(l => !string.IsNullOrWhiteSpace(l))
            .WithErrorCode(CatalogValidationCodes.CategoryRouteLocaleRequired);
        RuleFor(x => x.Slug)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithErrorCode(CatalogValidationCodes.CategoryRouteSlugRequired);
    }
}
