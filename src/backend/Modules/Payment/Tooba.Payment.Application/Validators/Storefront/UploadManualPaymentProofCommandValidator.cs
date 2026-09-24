using FluentValidation;
using Tooba.Payment.Application.Commands.UploadManualPaymentProof;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="UploadManualPaymentProofCommand"/>.
/// The content stream is only null-checked here and is never read or sought; file-size,
/// media-type policy and ownership stay in Application/Domain.
/// </summary>
public sealed class UploadManualPaymentProofCommandValidator : AbstractValidator<UploadManualPaymentProofCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public UploadManualPaymentProofCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        RuleFor(x => x.Content)
            .NotNull()
            .WithErrorCode(PaymentValidationCodes.ProofContentRequired);
        PaymentFluentRules.RequireNonBlank(this, x => x.FileName, PaymentValidationCodes.ProofFileNameShape);
        PaymentFluentRules.RequireNonBlank(this, x => x.ContentType, PaymentValidationCodes.ProofContentTypeShape);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
