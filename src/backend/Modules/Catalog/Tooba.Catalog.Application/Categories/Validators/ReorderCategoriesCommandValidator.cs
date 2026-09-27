using FluentValidation;
using Tooba.Catalog.Application.Categories.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Categories.Validators;

/// <summary>Transport shape for ReorderCategoriesCommand.</summary>
public sealed class ReorderCategoriesCommandValidator : AbstractValidator<ReorderCategoriesCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReorderCategoriesCommandValidator()
    {
        RuleFor(x => x.OrderedCategoryIds)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.CategoryOrderedIdsRequired);
    }
}
