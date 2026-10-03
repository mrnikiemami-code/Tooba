using FluentValidation;
using Tooba.Catalog.Application.Attributes.Definitions.Commands;

namespace Tooba.Catalog.Application.Attributes.Definitions.Validators;

/// <summary>Transport validator for UpdateAttributeDefinitionCommand.</summary>
public sealed class UpdateAttributeDefinitionCommandValidator : AbstractValidator<UpdateAttributeDefinitionCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateAttributeDefinitionCommandValidator()
    {
        RuleFor(x => x.DefinitionId).NotEmpty();
    }
}