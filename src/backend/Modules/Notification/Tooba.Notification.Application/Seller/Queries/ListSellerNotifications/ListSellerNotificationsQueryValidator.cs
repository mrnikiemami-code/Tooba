using FluentValidation;
using Tooba.Notification.Application.Validators;
using Tooba.Notification.Application.Seller.Queries.ListSellerNotifications;

namespace Tooba.Notification.Application.Seller.Queries.ListSellerNotifications;

/// <summary>
/// Transport-shape validation for <see cref="ListSellerNotificationsQuery"/>.
/// Paging bounds are clamped downstream by the directory (take 1..100, skip ≥ 0) — the validator
/// only rejects transport shapes the clamp cannot repair (non-positive take, negative skip).
/// The SellerPartyId is authorizer-derived and never policed.
/// </summary>
public sealed class ListSellerNotificationsQueryValidator : AbstractValidator<ListSellerNotificationsQuery>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public ListSellerNotificationsQueryValidator()
    {
        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100)
            .WithErrorCode(NotificationValidationCodes.SellerTakeOutOfRange);
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(NotificationValidationCodes.SellerSkipNegative);
    }
}
