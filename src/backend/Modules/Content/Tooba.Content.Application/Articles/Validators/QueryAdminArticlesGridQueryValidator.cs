using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Articles.Queries;

namespace Tooba.Content.Application.Articles.Validators;

public sealed class QueryAdminArticlesGridQueryValidator : AbstractValidator<QueryAdminArticlesGridQuery>
{
    public QueryAdminArticlesGridQueryValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(ContentValidationCodes.ReorderItemsRequired);
    }
}
