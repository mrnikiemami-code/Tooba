using FluentValidation;
using Tooba.Notification.Application.Validators;


namespace Tooba.Notification.Application.Customer.Queries;

/// <summary>
/// Transport-shape validation for <see cref="ListCustomerNotificationsQuery"/>.
/// Paging bounds are clamped downstream by the directory (take 1..100, skip ≥ 0) — the validator
/// only rejects transport shapes the clamp cannot repair (non-positive take, negative skip).
/// </summary>
public sealed class ListCustomerNotificationsQueryValidator : AbstractValidator<ListCustomerNotificationsQuery>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public ListCustomerNotificationsQueryValidator()
    {
        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100)
            .WithErrorCode(NotificationValidationCodes.CustomerTakeOutOfRange);
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(NotificationValidationCodes.CustomerSkipNegative);
    }
}
