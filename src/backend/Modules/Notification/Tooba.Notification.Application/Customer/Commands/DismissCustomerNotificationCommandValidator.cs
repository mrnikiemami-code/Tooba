using FluentValidation;
using Tooba.Notification.Application.Validators;


namespace Tooba.Notification.Application.Customer.Commands;

/// <summary>
/// Transport-shape validation for <see cref="DismissCustomerNotificationCommand"/>.
/// Ownership/existence stay Application-owned; only the primitive route identity is policed here.
/// </summary>
public sealed class DismissCustomerNotificationCommandValidator : AbstractValidator<DismissCustomerNotificationCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public DismissCustomerNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(NotificationValidationCodes.NotificationIdRequired);
    }
}
