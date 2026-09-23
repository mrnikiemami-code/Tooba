using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Shipping.Commands.SaveStorefrontShippingSelection;

public sealed class SaveStorefrontShippingSelectionCommandValidator : AbstractValidator<SaveStorefrontShippingSelectionCommand>
{
    public SaveStorefrontShippingSelectionCommandValidator()
    {
        RuleFor(x => x.Body).NotNull().WithErrorCode(OrderValidationCodes.ShippingBodyRequired);
        RuleFor(x => x.Body.CartId)
            .NotEmpty()
            .When(x => x.Body is not null)
            .WithErrorCode(OrderValidationCodes.CartIdRequired);
        RuleFor(x => x.Body.ExpectedCartVersion)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Body is not null)
            .WithErrorCode(OrderValidationCodes.ExpectedCartVersionMin);
    }
}