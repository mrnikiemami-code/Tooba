using FluentValidation;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Settings.CheckoutAbuse.Validators;

/// <summary>Transport: all three fields must be present (Host: settings.checkout_abuse.invalid).</summary>
public sealed class SaveCheckoutAbuseSettingsCommandValidator : AbstractValidator<SaveCheckoutAbuseSettingsCommand>
{
    /// <summary>Creates the validator.</summary>
    public SaveCheckoutAbuseSettingsCommandValidator()
    {
        RuleFor(x => x.MaxOpenUnpaidOrdersPerCustomer)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.CheckoutAbuseFieldsRequired);
        RuleFor(x => x.ReservationCommitWindowMinutes)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.CheckoutAbuseFieldsRequired);
        RuleFor(x => x.MaxCheckoutCommitsPerCustomerInWindow)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.CheckoutAbuseFieldsRequired);
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.CheckoutAbuseActorRequired);
    }
}
