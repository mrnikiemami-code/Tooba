using FluentValidation;
using Tooba.Notification.Application.Validators;


namespace Tooba.Notification.Application.Seller.Commands;

/// <summary>
/// Transport-shape validation for <see cref="MarkSellerNotificationReadCommand"/>.
/// Ownership/existence stay Application-owned; only the primitive route identity is policed here.
/// The SellerPartyId is authorizer-derived and never policed.
/// </summary>
public sealed class MarkSellerNotificationReadCommandValidator : AbstractValidator<MarkSellerNotificationReadCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public MarkSellerNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(NotificationValidationCodes.NotificationIdRequired);
    }
}
