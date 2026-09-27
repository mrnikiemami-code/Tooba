using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Authors.Commands;

namespace Tooba.Content.Application.Authors.Validators;

public sealed class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
{
    public CreateAuthorCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().WithErrorCode(ContentValidationCodes.DisplayNameRequired);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
    }
}
