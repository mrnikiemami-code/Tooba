using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Shipping.Queries.ProjectStorefrontShipping;

public sealed class ProjectStorefrontShippingQueryValidator : AbstractValidator<ProjectStorefrontShippingQuery>
{
    public ProjectStorefrontShippingQueryValidator()
    {
        OrderFluentRules.RequireCartId(this, x => x.CartId);
    }
}