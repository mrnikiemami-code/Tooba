using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Queries.ListAdminOrderReturnEligibility;

public sealed class ListAdminOrderReturnEligibilityQueryValidator : AbstractValidator<ListAdminOrderReturnEligibilityQuery>
{
    public ListAdminOrderReturnEligibilityQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
    }
}