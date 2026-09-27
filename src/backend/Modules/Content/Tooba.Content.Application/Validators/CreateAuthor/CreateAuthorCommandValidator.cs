using FluentValidation;
using Tooba.Content.Application.Commands.CreateAuthor;

namespace Tooba.Content.Application.Validators.CreateAuthor;

public sealed class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
{
    public CreateAuthorCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().WithErrorCode(ContentValidationCodes.DisplayNameRequired);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
    }
}
