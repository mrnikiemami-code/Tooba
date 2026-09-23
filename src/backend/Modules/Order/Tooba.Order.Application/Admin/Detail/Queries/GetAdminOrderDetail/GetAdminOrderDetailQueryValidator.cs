using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Detail.Queries.GetAdminOrderDetail;

/// <summary>Transport Guid primitives for admin order detail.</summary>
public sealed class GetAdminOrderDetailQueryValidator : AbstractValidator<GetAdminOrderDetailQuery>
{
    public GetAdminOrderDetailQueryValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.ViewerUserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
    }
}
