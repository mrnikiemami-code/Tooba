using FluentValidation;
using Tooba.Catalog.Application.Settings.HoldPolicy.Commands;
using Tooba.Catalog.Application.Validators;
using Tooba.Catalog.Contracts.Errors;

namespace Tooba.Catalog.Application.Settings.HoldPolicy.Validators;

/// <summary>Transport range checks for hold-policy PUT (Host HoldPolicySettingsEndpoints parity).</summary>
public sealed class SaveHoldPolicySettingsCommandValidator : AbstractValidator<SaveHoldPolicySettingsCommand>
{
    private const int MinHoldHours = 1;
    private const int MaxCartHours = 24 * 90;
    private const int MaxPaymentHours = 24 * 30;
    private const int MinReservationMinutes = 1;
    private const int MaxReservationMinutes = 24 * 60 * 30;
    private const int MinCycles = 1;
    private const int MaxCycles = 20;

    /// <summary>Creates the validator.</summary>
    public SaveHoldPolicySettingsCommandValidator()
    {
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.HoldPolicyActorRequired);

        RuleFor(x => x.CartPersistenceHours)
            .Must(v => HoursOk(v, MinHoldHours, MaxCartHours))
            .WithErrorCode(CatalogErrorCodes.HoldPolicyCartPersistenceInvalid);

        RuleFor(x => x.OnlinePaymentHoldHours)
            .Must(v => HoursOk(v, MinHoldHours, MaxPaymentHours))
            .WithErrorCode(CatalogErrorCodes.HoldPolicyOnlineInvalid);

        RuleFor(x => x.ManualPaymentInitialHoldHours)
            .Must(v => HoursOk(v, MinHoldHours, MaxPaymentHours))
            .WithErrorCode(CatalogErrorCodes.HoldPolicyManualInitialInvalid);

        RuleFor(x => x.ManualPaymentReviewHoldHours)
            .Must(v => HoursOk(v, MinHoldHours, MaxPaymentHours))
            .WithErrorCode(CatalogErrorCodes.HoldPolicyManualReviewInvalid);

        RuleFor(x => x.InitialReservationHoldMinutes)
            .Must(v => HoursOk(v, MinReservationMinutes, MaxReservationMinutes))
            .WithErrorCode(CatalogErrorCodes.HoldPolicyReservationInitialInvalid);

        RuleFor(x => x.RetryReservationHoldMinutes)
            .Must(v => HoursOk(v, MinReservationMinutes, MaxReservationMinutes))
            .WithErrorCode(CatalogErrorCodes.HoldPolicyReservationRetryInvalid);

        RuleFor(x => x.MaxReservationCycles)
            .Must(v => HoursOk(v, MinCycles, MaxCycles))
            .WithErrorCode(CatalogErrorCodes.HoldPolicyReservationMaxInvalid);

        When(x => x.Methods is not null, () =>
        {
            RuleForEach(x => x.Methods!)
                .ChildRules(method =>
                {
                    method.RuleFor(m => m.OnlinePaymentHoldHours)
                        .Must(v => HoursOk(v, MinHoldHours, MaxPaymentHours))
                        .WithErrorCode(CatalogErrorCodes.HoldPolicyMethodInvalid);
                    method.RuleFor(m => m.ManualPaymentInitialHoldHours)
                        .Must(v => HoursOk(v, MinHoldHours, MaxPaymentHours))
                        .WithErrorCode(CatalogErrorCodes.HoldPolicyMethodInvalid);
                    method.RuleFor(m => m.ManualPaymentReviewHoldHours)
                        .Must(v => HoursOk(v, MinHoldHours, MaxPaymentHours))
                        .WithErrorCode(CatalogErrorCodes.HoldPolicyMethodInvalid);
                });
        });
    }

    private static bool HoursOk(int? value, int min, int max) =>
        value is null || (value >= min && value <= max);
}
