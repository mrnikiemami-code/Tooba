using FluentValidation;
using Tooba.Catalog.Application.Attributes.ProductValues.Commands;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Validators;

/// <summary>Transport validator for SetProductAttributeCommand.</summary>
public sealed class SetProductAttributeCommandValidator : AbstractValidator<SetProductAttributeCommand>
{
    /// <summary>Creates the validator.</summary>
    public SetProductAttributeCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.DefinitionId).NotEmpty();
    }
}