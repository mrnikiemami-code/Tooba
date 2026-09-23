using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Supply.Queries.GetOrderSupplyStatus;

public sealed class GetOrderSupplyStatusQueryValidator : AbstractValidator<GetOrderSupplyStatusQuery>
{
    public GetOrderSupplyStatusQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
    }
}