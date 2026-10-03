using FluentValidation;
using Tooba.Catalog.Application.Attributes.Schema.Commands;

namespace Tooba.Catalog.Application.Attributes.Schema.Validators;

/// <summary>Transport validator for UnbindCategoryAttributeCommand.</summary>
public sealed class UnbindCategoryAttributeCommandValidator : AbstractValidator<UnbindCategoryAttributeCommand>
{
    /// <summary>Creates the validator.</summary>
    public UnbindCategoryAttributeCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.DefinitionId).NotEmpty();
    }
}