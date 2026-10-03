using FluentValidation;
using Tooba.Catalog.Application.Categories.Commands;

namespace Tooba.Catalog.Application.Categories.Validators;

/// <summary>Transport validator for UpdateCategoryCoreCommand.</summary>
public sealed class UpdateCategoryCoreCommandValidator : AbstractValidator<UpdateCategoryCoreCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateCategoryCoreCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}