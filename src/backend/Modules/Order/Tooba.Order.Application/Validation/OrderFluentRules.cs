using System.Linq.Expressions;
using FluentValidation;
using Tooba.Order.Application.Admin.Operations.Models;

namespace Tooba.Order.Application.Validation;

/// <summary>Reusable FluentValidation rule fragments for Order transport input.</summary>
public static class OrderFluentRules
{
    public const int MaxIdempotencyKeyLength = 128;
    public const int MaxTake = 200;
    public const int MinTake = 1;

    public static void RequireCheckoutId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OrderValidationCodes.CheckoutIdRequired);

    public static void RequireActorUserId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OrderValidationCodes.ActorUserIdRequired);

    public static void RequireSellerPartyId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OrderValidationCodes.SellerPartyIdRequired);

    public static void RequireCartId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OrderValidationCodes.CartIdRequired);

    public static void RequireNoteId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OrderValidationCodes.NoteIdRequired);

    public static void RequireSellerOrderId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OrderValidationCodes.SellerOrderIdRequired);

    public static void RequireExpectedCartVersionMin<T>(AbstractValidator<T> validator, Expression<Func<T, int>> selector)
        => validator.RuleFor(selector).GreaterThanOrEqualTo(0).WithErrorCode(OrderValidationCodes.ExpectedCartVersionMin);

    public static void RequireIdempotencyKey<T>(AbstractValidator<T> validator, Expression<Func<T, string>> selector)
    {
        validator.RuleFor(selector).NotEmpty().WithErrorCode(OrderValidationCodes.IdempotencyKeyRequired);
        validator.RuleFor(selector)
            .MaximumLength(MaxIdempotencyKeyLength)
            .WithErrorCode(OrderValidationCodes.IdempotencyKeyTooLong);
    }

    public static void RequireTakeRange<T>(AbstractValidator<T> validator, Expression<Func<T, int>> selector)
        => validator.RuleFor(selector)
            .InclusiveBetween(MinTake, MaxTake)
            .WithErrorCode(OrderValidationCodes.TakeRange);

    public static void RequireAdminOperationEnvelope<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> checkoutId,
        Expression<Func<T, Guid>> actorUserId,
        Expression<Func<T, AdminOrderOperationRequest>> request)
    {
        RequireCheckoutId(validator, checkoutId);
        RequireActorUserId(validator, actorUserId);
        validator.RuleFor(request).NotNull().WithErrorCode(OrderValidationCodes.OperationRequestRequired);
        validator.RuleFor(request)
            .Must(r => r is not null && !string.IsNullOrWhiteSpace(r.Code))
            .WithErrorCode(OrderValidationCodes.OperationCodeRequired);
    }
}
