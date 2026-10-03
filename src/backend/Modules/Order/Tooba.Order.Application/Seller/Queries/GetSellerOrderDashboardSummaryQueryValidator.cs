using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Seller.Queries;

/// <summary>Transport Guid primitives for the seller dashboard summary.</summary>
public sealed class GetSellerOrderDashboardSummaryQueryValidator
    : AbstractValidator<GetSellerOrderDashboardSummaryQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetSellerOrderDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.SellerPartyId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.SellerPartyIdRequired);
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
    }
}
