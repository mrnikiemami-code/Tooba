using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Customer.Queries.GetCustomerOrderDetail;

/// <summary>Transport Guid primitives for customer order detail.</summary>
public sealed class GetCustomerOrderDetailQueryValidator : AbstractValidator<GetCustomerOrderDetailQuery>
{
    public GetCustomerOrderDetailQueryValidator()
    {
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
    }
}
