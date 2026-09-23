using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Customer.Queries.ListCustomerOrders;

public sealed class ListCustomerOrdersQueryValidator : AbstractValidator<ListCustomerOrdersQuery>
{
    public ListCustomerOrdersQueryValidator()
    {
        OrderFluentRules.RequireActorUserId(this, x => x.ActorUserId);
    }
}