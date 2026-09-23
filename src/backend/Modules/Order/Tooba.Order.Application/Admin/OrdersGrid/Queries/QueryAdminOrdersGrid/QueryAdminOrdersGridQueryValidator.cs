using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.OrdersGrid.Queries.QueryAdminOrdersGrid;

/// <summary>Requires a grid request object; field whitelist stays in grid policy/handler.</summary>
public sealed class QueryAdminOrdersGridQueryValidator : AbstractValidator<QueryAdminOrdersGridQuery>
{
    public QueryAdminOrdersGridQueryValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(OrderValidationCodes.GridRequestRequired);
    }
}
