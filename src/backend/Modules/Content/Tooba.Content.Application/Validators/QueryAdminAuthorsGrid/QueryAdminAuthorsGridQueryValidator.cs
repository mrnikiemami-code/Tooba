using FluentValidation;
using Tooba.Content.Application.Queries.QueryAdminAuthorsGrid;

namespace Tooba.Content.Application.Validators.QueryAdminAuthorsGrid;

public sealed class QueryAdminAuthorsGridQueryValidator : AbstractValidator<QueryAdminAuthorsGridQuery>
{
    public QueryAdminAuthorsGridQueryValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(ContentValidationCodes.ReorderItemsRequired);
    }
}
