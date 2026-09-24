using FluentValidation;
using Tooba.Payment.Application.Commands.SubmitManualPaymentEvidence;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="SubmitManualPaymentEvidenceCommand"/>.
/// Transfer verification, ownership and payment state stay in Application/Domain.
/// </summary>
public sealed class SubmitManualPaymentEvidenceCommandValidator
    : AbstractValidator<SubmitManualPaymentEvidenceCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public SubmitManualPaymentEvidenceCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.RequireNonBlank(
            this, x => x.TransferReference, PaymentValidationCodes.TransferReferenceShape);
        PaymentFluentRules.OptionalIdShape(
            this, x => x.ProofMediaAssetId, PaymentValidationCodes.ProofMediaAssetIdShape);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
