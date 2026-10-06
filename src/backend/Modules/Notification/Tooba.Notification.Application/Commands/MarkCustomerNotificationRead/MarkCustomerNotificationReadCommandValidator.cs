using FluentValidation;
using Tooba.Notification.Application.Validators;
using Tooba.Notification.Application.Commands.MarkCustomerNotificationRead;

namespace Tooba.Notification.Application.Commands.MarkCustomerNotificationRead;

/// <summary>
/// Transport-shape validation for <see cref="MarkCustomerNotificationReadCommand"/>.
/// Ownership/existence stay Application-owned; only the primitive route identity is policed here.
/// </summary>
public sealed class MarkCustomerNotificationReadCommandValidator : AbstractValidator<MarkCustomerNotificationReadCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public MarkCustomerNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(NotificationValidationCodes.NotificationIdRequired);
    }
}
