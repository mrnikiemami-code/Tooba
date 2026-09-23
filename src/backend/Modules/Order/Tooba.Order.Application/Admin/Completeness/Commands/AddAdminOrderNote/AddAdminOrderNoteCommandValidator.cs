using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Completeness.Commands.AddAdminOrderNote;

/// <summary>Syntactic input rules for admin note create (business existence stays in handler).</summary>
public sealed class AddAdminOrderNoteCommandValidator : AbstractValidator<AddAdminOrderNoteCommand>
{
    public const int MaxBodyLength = 4000;

    public AddAdminOrderNoteCommandValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.Actor.UserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
        RuleFor(x => x.Body)
            .Must(b => !string.IsNullOrWhiteSpace(b))
            .WithErrorCode(OrderValidationCodes.NoteBodyRequired);
        RuleFor(x => x.Body)
            .MaximumLength(MaxBodyLength)
            .When(x => x.Body is not null)
            .WithErrorCode(OrderValidationCodes.NoteBodyTooLong);
    }
}
