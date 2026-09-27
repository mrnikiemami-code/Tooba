using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Categories.Commands;

namespace Tooba.Content.Application.Categories.Validators;

public sealed class UpdateCategoryMediaCommandValidator : AbstractValidator<UpdateCategoryMediaCommand>
{
    public UpdateCategoryMediaCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithErrorCode(ContentValidationCodes.CategoryIdRequired);
    }
}
