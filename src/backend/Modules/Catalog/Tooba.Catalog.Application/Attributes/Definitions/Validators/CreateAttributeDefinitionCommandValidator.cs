using FluentValidation;
using Tooba.Catalog.Application.Attributes.Definitions.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Attributes.Definitions.Validators;

/// <summary>Transport shape for CreateAttributeDefinitionCommand.</summary>
public sealed class CreateAttributeDefinitionCommandValidator
    : AbstractValidator<CreateAttributeDefinitionCommand>
{
    /// <summary>Creates the validator.</summary>
    public CreateAttributeDefinitionCommandValidator()
    {
        RuleFor(x => x.Model)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.AttributeDefinitionCodeRequired);
        RuleFor(x => x.Model!.Code)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.AttributeDefinitionCodeRequired)
            .When(x => x.Model is not null);
    }
}
