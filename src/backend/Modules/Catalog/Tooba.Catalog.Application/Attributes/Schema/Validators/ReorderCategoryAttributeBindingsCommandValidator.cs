using FluentValidation;
using Tooba.Catalog.Application.Attributes.Schema.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Attributes.Schema.Validators;

/// <summary>Transport shape for ReorderCategoryAttributeBindingsCommand.</summary>
public sealed class ReorderCategoryAttributeBindingsCommandValidator
    : AbstractValidator<ReorderCategoryAttributeBindingsCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReorderCategoryAttributeBindingsCommandValidator()
    {
        RuleFor(x => x.OrderedDefinitionIds)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.SchemaOrderedDefinitionIdsRequired);
    }
}
