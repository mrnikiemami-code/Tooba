using FluentValidation;
using Tooba.Content.Application.Commands.CreateCategory;

namespace Tooba.Content.Application.Validators.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().WithErrorCode(ContentValidationCodes.LanguageCodeRequired);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode(ContentValidationCodes.NameRequired);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
    }
}
