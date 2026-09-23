using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Seller.Queries.GetSellerOrderDetail;

/// <summary>Transport Guid primitives for seller order detail.</summary>
public sealed class GetSellerOrderDetailQueryValidator : AbstractValidator<GetSellerOrderDetailQuery>
{
    public GetSellerOrderDetailQueryValidator()
    {
        RuleFor(x => x.SellerPartyId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.SellerPartyIdRequired);
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
        RuleFor(x => x.SellerOrderId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.SellerOrderIdRequired);
    }
}
