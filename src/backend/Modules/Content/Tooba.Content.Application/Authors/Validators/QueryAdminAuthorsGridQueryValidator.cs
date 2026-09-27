using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Authors.Queries;

namespace Tooba.Content.Application.Authors.Validators;

public sealed class QueryAdminAuthorsGridQueryValidator : AbstractValidator<QueryAdminAuthorsGridQuery>
{
    public QueryAdminAuthorsGridQueryValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(ContentValidationCodes.ReorderItemsRequired);
    }
}
