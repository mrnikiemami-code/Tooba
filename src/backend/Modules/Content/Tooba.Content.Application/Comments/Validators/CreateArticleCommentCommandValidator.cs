using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Comments.Commands;

namespace Tooba.Content.Application.Comments.Validators;

public sealed class CreateArticleCommentCommandValidator : AbstractValidator<CreateArticleCommentCommand>
{
    public CreateArticleCommentCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.DisplayName).NotEmpty().WithErrorCode(ContentValidationCodes.DisplayNameRequired);
        RuleFor(x => x.Body).NotEmpty().WithErrorCode(ContentValidationCodes.CommentBodyRequired);
    }
}
