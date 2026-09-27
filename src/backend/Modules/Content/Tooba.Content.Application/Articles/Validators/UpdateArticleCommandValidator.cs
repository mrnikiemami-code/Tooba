using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Articles.Commands;

namespace Tooba.Content.Application.Articles.Validators;

public sealed class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
    public UpdateArticleCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.Title).NotEmpty().WithErrorCode(ContentValidationCodes.TitleRequired);
        RuleFor(x => x.Excerpt).NotEmpty().WithErrorCode(ContentValidationCodes.ExcerptRequired);
        RuleFor(x => x.Body).NotEmpty().WithErrorCode(ContentValidationCodes.BodyRequired);
    }
}
