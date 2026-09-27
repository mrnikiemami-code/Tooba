using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Categories.Commands;

namespace Tooba.Content.Application.Categories.Validators;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().WithErrorCode(ContentValidationCodes.LanguageCodeRequired);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode(ContentValidationCodes.NameRequired);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
    }
}
