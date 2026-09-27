using FluentValidation;
using Tooba.Catalog.Application.Attributes.Definitions.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Attributes.Definitions.Validators;

/// <summary>Transport shape for AddAttributeOptionCommand.</summary>
public sealed class AddAttributeOptionCommandValidator : AbstractValidator<AddAttributeOptionCommand>
{
    /// <summary>Creates the validator.</summary>
    public AddAttributeOptionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.AttributeOptionCodeRequired);
    }
}
