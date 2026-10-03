using FluentValidation;
using Tooba.Catalog.Application.Attributes.Schema.Commands;

namespace Tooba.Catalog.Application.Attributes.Schema.Validators;

/// <summary>Transport validator for UpdateCategoryAttributeBindingCommand.</summary>
public sealed class UpdateCategoryAttributeBindingCommandValidator : AbstractValidator<UpdateCategoryAttributeBindingCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateCategoryAttributeBindingCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.DefinitionId).NotEmpty();
    }
}