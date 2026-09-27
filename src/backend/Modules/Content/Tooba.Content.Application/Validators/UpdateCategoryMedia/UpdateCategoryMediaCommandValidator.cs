using FluentValidation;
using Tooba.Content.Application.Commands.UpdateCategoryMedia;

namespace Tooba.Content.Application.Validators.UpdateCategoryMedia;

public sealed class UpdateCategoryMediaCommandValidator : AbstractValidator<UpdateCategoryMediaCommand>
{
    public UpdateCategoryMediaCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithErrorCode(ContentValidationCodes.CategoryIdRequired);
    }
}
