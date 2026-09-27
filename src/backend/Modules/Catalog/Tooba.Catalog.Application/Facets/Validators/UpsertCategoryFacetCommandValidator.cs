using FluentValidation;
using Tooba.Catalog.Application.Facets.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Facets.Validators;

/// <summary>Transport shape for UpsertCategoryFacetCommand.</summary>
public sealed class UpsertCategoryFacetCommandValidator : AbstractValidator<UpsertCategoryFacetCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpsertCategoryFacetCommandValidator()
    {
        RuleFor(x => x.Input)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.FacetInputRequired);
    }
}
