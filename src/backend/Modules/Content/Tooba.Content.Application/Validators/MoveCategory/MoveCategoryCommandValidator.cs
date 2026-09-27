using FluentValidation;
using Tooba.Content.Application.Commands.MoveCategory;

namespace Tooba.Content.Application.Validators.MoveCategory;

public sealed class MoveCategoryCommandValidator : AbstractValidator<MoveCategoryCommand>
{
    public MoveCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithErrorCode(ContentValidationCodes.CategoryIdRequired);
    }
}
