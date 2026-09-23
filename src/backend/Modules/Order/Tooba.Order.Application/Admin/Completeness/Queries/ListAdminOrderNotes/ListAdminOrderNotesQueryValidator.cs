using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Completeness.Queries.ListAdminOrderNotes;

public sealed class ListAdminOrderNotesQueryValidator : AbstractValidator<ListAdminOrderNotesQuery>
{
    public ListAdminOrderNotesQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireActorUserId(this, x => x.Actor.UserId);
    }
}