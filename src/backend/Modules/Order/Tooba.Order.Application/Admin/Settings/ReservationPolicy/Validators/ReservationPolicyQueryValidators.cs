using FluentValidation;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Validators;

/// <summary>Transport range checks for Admin GET reservation-policy audit (Host parity).</summary>
public sealed class GetReservationPolicyAuditQueryValidator
    : AbstractValidator<GetReservationPolicyAuditQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetReservationPolicyAuditQueryValidator() =>
        RuleFor(x => x.Take)
            .InclusiveBetween(OrderFluentRules.MinTake, OrderFluentRules.MaxTake)
            .When(x => x.Take.HasValue)
            .WithErrorCode(OrderValidationCodes.TakeRange);
}

/// <summary>Transport id checks for Admin GET offer reservation-policy preview.</summary>
public sealed class GetOfferReservationPolicyQueryValidator
    : AbstractValidator<GetOfferReservationPolicyQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetOfferReservationPolicyQueryValidator() =>
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.OfferIdRequired);
}

/// <summary>Transport id checks for Admin GET category reservation-policy preview.</summary>
public sealed class GetCategoryReservationPolicyQueryValidator
    : AbstractValidator<GetCategoryReservationPolicyQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetCategoryReservationPolicyQueryValidator() =>
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CategoryIdRequired);
}

/// <summary>Transport id checks for Seller GET offer reservation-policy (read-only).</summary>
public sealed class GetSellerOfferReservationPolicyQueryValidator
    : AbstractValidator<GetSellerOfferReservationPolicyQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetSellerOfferReservationPolicyQueryValidator() =>
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.OfferIdRequired);
}

/// <summary>Transport id checks for Seller PUT offer reservation-policy (always denied).</summary>
public sealed class DenySellerOfferReservationPolicyCommandValidator
    : AbstractValidator<DenySellerOfferReservationPolicyCommand>
{
    /// <summary>Creates the validator.</summary>
    public DenySellerOfferReservationPolicyCommandValidator() =>
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.OfferIdRequired);
}
