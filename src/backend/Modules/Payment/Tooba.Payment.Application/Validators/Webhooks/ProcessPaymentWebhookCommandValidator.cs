using FluentValidation;
using Tooba.Payment.Application.Commands.ProcessPaymentWebhook;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Webhooks;

/// <summary>
/// Transport envelope validation for <see cref="ProcessPaymentWebhookCommand"/>.
/// JSON parsing, payload field shape, signature verification and provider semantics stay in the
/// existing handler and <c>IPaymentWebhookSignatureVerifier</c> boundary.
/// </summary>
public sealed class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    /// <summary>Registers primitive-shape rules for the webhook envelope.</summary>
    public ProcessPaymentWebhookCommandValidator()
    {
        PaymentFluentRules.RequireNonBlank(
            this, x => x.ProviderCode, PaymentValidationCodes.WebhookProviderCodeShape);

        RuleFor(x => x.RawBody)
            .NotNull()
            .Must(body => body is not null && body.Length > 0)
            .WithErrorCode(PaymentValidationCodes.WebhookRawBodyRequired);

        PaymentFluentRules.RequireNonBlank(this, x => x.BodyText, PaymentValidationCodes.WebhookBodyTextShape);

        PaymentFluentRules.OptionalNonBlankShape(
            this, x => x.SignatureHeader, PaymentValidationCodes.WebhookSignatureHeaderShape);
    }
}
