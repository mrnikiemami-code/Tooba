using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Checkout.Commands.SubmitStorefrontCheckout;

/// <summary>Syntactic checkout submit input (abuse/inventory/business stay outside).</summary>
public sealed class SubmitStorefrontCheckoutCommandValidator : AbstractValidator<SubmitStorefrontCheckoutCommand>
{
    public const int MaxIdempotencyKeyLength = 128;

    public SubmitStorefrontCheckoutCommandValidator()
    {
        RuleFor(x => x.CartId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CartIdRequired);
        RuleFor(x => x.ExpectedCartVersion)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(OrderValidationCodes.ExpectedCartVersionMin);
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.IdempotencyKeyRequired);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(MaxIdempotencyKeyLength)
            .When(x => x.IdempotencyKey is not null)
            .WithErrorCode(OrderValidationCodes.IdempotencyKeyTooLong);
    }
}
