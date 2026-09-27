using FluentValidation;
using Tooba.Catalog.Application.Attributes.Schema.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Attributes.Schema.Validators;

/// <summary>Transport shape for BindCategoryAttributeCommand.</summary>
public sealed class BindCategoryAttributeCommandValidator : AbstractValidator<BindCategoryAttributeCommand>
{
    /// <summary>Creates the validator.</summary>
    public BindCategoryAttributeCommandValidator()
    {
        RuleFor(x => x.Model)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.SchemaDefinitionIdRequired);
        RuleFor(x => x.Model!.DefinitionId)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.SchemaDefinitionIdRequired)
            .When(x => x.Model is not null);
    }
}
