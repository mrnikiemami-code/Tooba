using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Articles.Commands;

namespace Tooba.Content.Application.Articles.Validators;

public sealed class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ContentValidationCodes.SlugRequired);
        RuleFor(x => x.Title).NotEmpty().WithErrorCode(ContentValidationCodes.TitleRequired);
        RuleFor(x => x.Excerpt).NotEmpty().WithErrorCode(ContentValidationCodes.ExcerptRequired);
        RuleFor(x => x.Body).NotEmpty().WithErrorCode(ContentValidationCodes.BodyRequired);
    }
}
