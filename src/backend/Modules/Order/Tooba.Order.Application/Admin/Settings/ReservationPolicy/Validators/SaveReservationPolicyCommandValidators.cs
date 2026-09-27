using FluentValidation;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Commands;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Validators;

/// <summary>Transport range checks for store reservation-policy PUT (Host parity).</summary>
public sealed class SaveStoreReservationPolicyCommandValidator
    : AbstractValidator<SaveStoreReservationPolicyCommand>
{
    /// <summary>Creates the validator.</summary>
    public SaveStoreReservationPolicyCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.InitialReservationHoldMinutes)
            .Must(ReservationPolicyWriteRules.MinutesOk)
            .WithErrorCode(ReservationPolicyErrors.InitialInvalid)
            .WithMessage("مدت رزرو اولیه باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        RuleFor(x => x.RetryReservationHoldMinutes)
            .Must(ReservationPolicyWriteRules.MinutesOk)
            .WithErrorCode(ReservationPolicyErrors.RetryInvalid)
            .WithMessage("مدت رزرو مجدد باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        RuleFor(x => x.MaxReservationCycles)
            .Must(ReservationPolicyWriteRules.CyclesOk)
            .WithErrorCode(ReservationPolicyErrors.MaxInvalid)
            .WithMessage("حداکثر دفعات رزرو باید عددی صحیح بین ۱ و ۲۰ باشد.");
    }
}

/// <summary>Transport range checks for category reservation-policy PUT.</summary>
public sealed class SaveCategoryReservationPolicyCommandValidator
    : AbstractValidator<SaveCategoryReservationPolicyCommand>
{
    /// <summary>Creates the validator.</summary>
    public SaveCategoryReservationPolicyCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.InitialReservationHoldMinutes)
            .Must(ReservationPolicyWriteRules.MinutesOk)
            .WithErrorCode(ReservationPolicyErrors.InitialInvalid)
            .WithMessage("مدت رزرو اولیه باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        RuleFor(x => x.RetryReservationHoldMinutes)
            .Must(ReservationPolicyWriteRules.MinutesOk)
            .WithErrorCode(ReservationPolicyErrors.RetryInvalid)
            .WithMessage("مدت رزرو مجدد باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        RuleFor(x => x.MaxReservationCycles)
            .Must(ReservationPolicyWriteRules.CyclesOk)
            .WithErrorCode(ReservationPolicyErrors.MaxInvalid)
            .WithMessage("حداکثر دفعات رزرو باید عددی صحیح بین ۱ و ۲۰ باشد.");
    }
}

/// <summary>Transport range checks for offer reservation-policy PUT.</summary>
public sealed class SaveOfferReservationPolicyCommandValidator
    : AbstractValidator<SaveOfferReservationPolicyCommand>
{
    /// <summary>Creates the validator.</summary>
    public SaveOfferReservationPolicyCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.InitialReservationHoldMinutes)
            .Must(ReservationPolicyWriteRules.MinutesOk)
            .WithErrorCode(ReservationPolicyErrors.InitialInvalid)
            .WithMessage("مدت رزرو اولیه باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        RuleFor(x => x.RetryReservationHoldMinutes)
            .Must(ReservationPolicyWriteRules.MinutesOk)
            .WithErrorCode(ReservationPolicyErrors.RetryInvalid)
            .WithMessage("مدت رزرو مجدد باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        RuleFor(x => x.MaxReservationCycles)
            .Must(ReservationPolicyWriteRules.CyclesOk)
            .WithErrorCode(ReservationPolicyErrors.MaxInvalid)
            .WithMessage("حداکثر دفعات رزرو باید عددی صحیح بین ۱ و ۲۰ باشد.");
    }
}

internal static class ReservationPolicyWriteRules
{
    public static bool MinutesOk(int? value) =>
        value is null
        || (value >= ReservationPolicyComposer.MinMinutes && value <= ReservationPolicyComposer.MaxMinutes);

    public static bool CyclesOk(int? value) =>
        value is null
        || (value >= ReservationPolicyComposer.MinCycles && value <= ReservationPolicyComposer.MaxCycles);
}
