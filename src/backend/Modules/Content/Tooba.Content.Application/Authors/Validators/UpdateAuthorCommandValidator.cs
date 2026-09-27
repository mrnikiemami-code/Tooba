using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Authors.Commands;

namespace Tooba.Content.Application.Authors.Validators;

public sealed class UpdateAuthorCommandValidator : AbstractValidator<UpdateAuthorCommand>
{
    public UpdateAuthorCommandValidator()
    {
        RuleFor(x => x.AuthorId).NotEmpty().WithErrorCode(ContentValidationCodes.AuthorIdRequired);
        RuleFor(x => x.DisplayName).NotEmpty().WithErrorCode(ContentValidationCodes.DisplayNameRequired);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
    }
}
