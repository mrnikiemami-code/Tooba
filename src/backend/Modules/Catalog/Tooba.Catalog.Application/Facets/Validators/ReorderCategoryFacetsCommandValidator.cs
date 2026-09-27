using FluentValidation;
using Tooba.Catalog.Application.Facets.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Facets.Validators;

/// <summary>Transport shape for ReorderCategoryFacetsCommand.</summary>
public sealed class ReorderCategoryFacetsCommandValidator : AbstractValidator<ReorderCategoryFacetsCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReorderCategoryFacetsCommandValidator()
    {
        RuleFor(x => x.OrderedDefinitionIds)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.FacetOrderedDefinitionIdsRequired);
    }
}
