using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Completeness.Commands.DeleteAdminOrderNote;

public sealed class DeleteAdminOrderNoteCommandValidator : AbstractValidator<DeleteAdminOrderNoteCommand>
{
    public DeleteAdminOrderNoteCommandValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireNoteId(this, x => x.NoteId);
        OrderFluentRules.RequireActorUserId(this, x => x.Actor.UserId);
    }
}