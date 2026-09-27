using FluentValidation;
using Tooba.Content.Application.Commands.UpdateCategory;

namespace Tooba.Content.Application.Validators.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithErrorCode(ContentValidationCodes.CategoryIdRequired);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode(ContentValidationCodes.NameRequired);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
    }
}
