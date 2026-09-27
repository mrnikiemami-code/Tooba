using FluentValidation;
using Tooba.Content.Application.Commands.UpdateAuthor;

namespace Tooba.Content.Application.Validators.UpdateAuthor;

public sealed class UpdateAuthorCommandValidator : AbstractValidator<UpdateAuthorCommand>
{
    public UpdateAuthorCommandValidator()
    {
        RuleFor(x => x.AuthorId).NotEmpty().WithErrorCode(ContentValidationCodes.AuthorIdRequired);
        RuleFor(x => x.DisplayName).NotEmpty().WithErrorCode(ContentValidationCodes.DisplayNameRequired);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
    }
}
