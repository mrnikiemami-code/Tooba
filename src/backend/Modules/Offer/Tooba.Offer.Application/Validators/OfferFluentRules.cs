using System.Linq.Expressions;
using FluentValidation;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Offer.Application.Validators;

/// <summary>
/// Reusable FluentValidation fragments for Offer transport input.
/// Only primitive/syntactic shape is checked here; catalog/seller existence, DB uniqueness,
/// return-policy governance and domain quantity invariants stay in Application/Domain.
/// </summary>
public static class OfferFluentRules
{
    /// <summary>Requires a non-empty offer identifier.</summary>
    public static void RequireOfferId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OfferValidationCodes.OfferIdRequired);

    /// <summary>Requires a non-empty seller party identifier.</summary>
    public static void RequireSellerPartyId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OfferValidationCodes.SellerPartyIdRequired);

    /// <summary>Requires a non-empty catalog variant identifier.</summary>
    public static void RequireCatalogVariantId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(OfferValidationCodes.CatalogVariantIdRequired);

    /// <summary>Requires a defined contract enum value.</summary>
    public static void RequireDefinedEnum<T, TEnum>(AbstractValidator<T> validator, Expression<Func<T, TEnum>> selector)
        where TEnum : struct, Enum
        => validator.RuleFor(selector).IsInEnum().WithErrorCode(OfferValidationCodes.ChannelShape);

    /// <summary>
    /// Optional seller SKU: absent is allowed; a supplied value must not be whitespace-only.
    /// The canonical seller-SKU storage limit remains the persistence/domain concern.
    /// </summary>
    public static void OptionalSellerSkuShape<T>(AbstractValidator<T> validator, Expression<Func<T, string?>> selector)
        => validator.RuleFor(selector)
            .Must(value => value is null || value.Trim().Length > 0)
            .WithErrorCode(OfferValidationCodes.SellerSkuShape);

    /// <summary>Optional status restricted to the supplied canonical set.</summary>
    public static void OptionalStatusIn<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        IReadOnlyCollection<string> allowed,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || allowed.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithErrorCode(errorCode);

    /// <summary>Optional return policy choice restricted to the canonical contract choices.</summary>
    public static void OptionalReturnPolicyChoiceShape<T>(AbstractValidator<T> validator, Expression<Func<T, string?>> selector)
        => validator.RuleFor(selector)
            .Must(value => string.IsNullOrWhiteSpace(value) || IsSupportedChoice(value))
            .WithErrorCode(OfferValidationCodes.ReturnPolicyChoiceShape);

    /// <summary>Optional custom return window: when present it must be at least one day.</summary>
    public static void OptionalCustomReturnWindowMin<T>(AbstractValidator<T> validator, Expression<Func<T, int?>> selector)
        => validator.RuleFor(selector)
            .Must(days => days is null || days.Value >= 1)
            .WithErrorCode(OfferValidationCodes.CustomReturnWindowMin);

    /// <summary>
    /// Optional order quantity limits: each positive when supplied and min &lt;= max when both supplied.
    /// The domain re-asserts the same invariant; transport only shapes the request.
    /// </summary>
    public static void OptionalOrderQuantityLimits<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, decimal?>> minSelector,
        Expression<Func<T, decimal?>> maxSelector)
    {
        var readMax = maxSelector.Compile();
        validator.RuleFor(minSelector)
            .Must(minimum => minimum is null || minimum.Value > 0)
            .WithErrorCode(OfferValidationCodes.MinimumOrderQuantityMin);
        validator.RuleFor(maxSelector)
            .Must(maximum => maximum is null || maximum.Value > 0)
            .WithErrorCode(OfferValidationCodes.MaximumOrderQuantityMin);
        validator.RuleFor(minSelector)
            .Must((model, minimum) => minimum is null
                || readMax(model) is not { } maximum
                || minimum.Value <= maximum)
            .WithErrorCode(OfferValidationCodes.OrderQuantityRange);
    }

    /// <summary>Optional non-blank text: absent is allowed; a supplied value must not be whitespace-only.</summary>
    public static void OptionalNonBlankShape<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => value is null || value.Trim().Length > 0)
            .WithErrorCode(errorCode);

    private static bool IsSupportedChoice(string value)
    {
        var trimmed = value.Trim();
        return trimmed == OfferReturnPolicyChoices.Default
            || trimmed == OfferReturnPolicyChoices.Custom
            || trimmed == OfferReturnPolicyChoices.NonReturnable;
    }
}
