using FluentValidation;
using Tooba.Content.Application.Commands.CreateTag;

namespace Tooba.Content.Application.Validators.CreateTag;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().WithErrorCode(ContentValidationCodes.LanguageCodeRequired);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode(ContentValidationCodes.NameRequired);
    }
}
