using FluentValidation;
using Tooba.Content.Application.Commands.UpdateArticle;

namespace Tooba.Content.Application.Validators.UpdateArticle;

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
