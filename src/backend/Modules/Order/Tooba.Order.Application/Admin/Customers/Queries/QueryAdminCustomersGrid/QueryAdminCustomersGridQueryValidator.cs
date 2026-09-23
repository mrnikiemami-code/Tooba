using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Customers.Queries.QueryAdminCustomersGrid;

public sealed class QueryAdminCustomersGridQueryValidator : AbstractValidator<QueryAdminCustomersGridQuery>
{
    public QueryAdminCustomersGridQueryValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(OrderValidationCodes.GridRequestRequired);
    }
}