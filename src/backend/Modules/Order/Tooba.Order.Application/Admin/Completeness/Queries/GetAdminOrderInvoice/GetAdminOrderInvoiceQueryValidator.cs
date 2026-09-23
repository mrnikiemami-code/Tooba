using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderInvoice;

public sealed class GetAdminOrderInvoiceQueryValidator : AbstractValidator<GetAdminOrderInvoiceQuery>
{
    public GetAdminOrderInvoiceQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireActorUserId(this, x => x.Actor.UserId);
    }
}