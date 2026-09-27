using FluentValidation;
using Tooba.Content.Application.Queries.QueryAdminArticlesGrid;

namespace Tooba.Content.Application.Validators.QueryAdminArticlesGrid;

public sealed class QueryAdminArticlesGridQueryValidator : AbstractValidator<QueryAdminArticlesGridQuery>
{
    public QueryAdminArticlesGridQueryValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(ContentValidationCodes.ReorderItemsRequired);
    }
}
