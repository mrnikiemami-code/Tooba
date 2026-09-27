using FluentValidation;
using Tooba.Content.Application.Commands.CreateArticle;

namespace Tooba.Content.Application.Validators.CreateArticle;

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
