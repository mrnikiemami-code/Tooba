using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Tags.Commands;

namespace Tooba.Content.Application.Tags.Validators;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().WithErrorCode(ContentValidationCodes.LanguageCodeRequired);
        RuleFor(x => x.Name).NotEmpty().WithErrorCode(ContentValidationCodes.NameRequired);
    }
}
