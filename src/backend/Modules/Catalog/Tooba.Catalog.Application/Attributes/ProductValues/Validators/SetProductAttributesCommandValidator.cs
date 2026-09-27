using FluentValidation;
using Tooba.Catalog.Application.Attributes.ProductValues.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Validators;

/// <summary>Transport shape for SetProductAttributesCommand.</summary>
public sealed class SetProductAttributesCommandValidator : AbstractValidator<SetProductAttributesCommand>
{
    /// <summary>Creates the validator.</summary>
    public SetProductAttributesCommandValidator()
    {
        RuleFor(x => x.Model)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.ProductAttributeValuesRequired);
        RuleFor(x => x.Model!.Values)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.ProductAttributeValuesRequired)
            .When(x => x.Model is not null);
        RuleForEach(x => x.Model!.Values)
            .ChildRules(item =>
            {
                item.RuleFor(v => v.DefinitionId)
                    .NotEmpty()
                    .WithErrorCode(CatalogValidationCodes.ProductAttributeDefinitionIdRequired);
            })
            .When(x => x.Model?.Values is not null);
    }
}
