using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderReceipt;

public sealed class GetAdminOrderReceiptQueryValidator : AbstractValidator<GetAdminOrderReceiptQuery>
{
    public GetAdminOrderReceiptQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireActorUserId(this, x => x.Actor.UserId);
    }
}