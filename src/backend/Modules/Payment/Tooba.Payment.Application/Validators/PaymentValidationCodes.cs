namespace Tooba.Payment.Application.Validators;

/// <summary>Stable machine codes for Payment FluentValidation transport input (never localized identity).</summary>
public static class PaymentValidationCodes
{
    /// <summary>Payment identifier must be provided.</summary>
    public const string PaymentIdRequired = "payment.validation.payment_id_required";

    /// <summary>Cart identifier must be provided.</summary>
    public const string CartIdRequired = "payment.validation.cart_id_required";

    /// <summary>Checkout identifier must be provided.</summary>
    public const string CheckoutIdRequired = "payment.validation.checkout_id_required";

    /// <summary>Payment attempt identifier must be provided.</summary>
    public const string AttemptIdRequired = "payment.validation.attempt_id_required";

    /// <summary>Idempotency key must not be blank.</summary>
    public const string IdempotencyKeyShape = "payment.validation.idempotency_key_shape";

    /// <summary>Authenticated user identifier must be non-empty when supplied.</summary>
    public const string AuthenticatedUserIdShape = "payment.validation.authenticated_user_id_shape";

    /// <summary>Guest secret must not be blank when supplied.</summary>
    public const string GuestSecretShape = "payment.validation.guest_secret_shape";

    /// <summary>Provider code must not be blank when supplied.</summary>
    public const string ProviderCodeShape = "payment.validation.provider_code_shape";

    /// <summary>Provider request reference must not be blank.</summary>
    public const string ProviderRequestReferenceShape = "payment.validation.provider_request_reference_shape";

    /// <summary>Sandbox outcome must not be blank.</summary>
    public const string OutcomeShape = "payment.validation.outcome_shape";

    /// <summary>Transfer reference must not be blank.</summary>
    public const string TransferReferenceShape = "payment.validation.transfer_reference_shape";

    /// <summary>Proof media asset identifier must be non-empty when supplied.</summary>
    public const string ProofMediaAssetIdShape = "payment.validation.proof_media_asset_id_shape";

    /// <summary>Proof content stream must be supplied.</summary>
    public const string ProofContentRequired = "payment.validation.proof_content_required";

    /// <summary>Proof file name must not be blank.</summary>
    public const string ProofFileNameShape = "payment.validation.proof_file_name_shape";

    /// <summary>Proof content type must not be blank.</summary>
    public const string ProofContentTypeShape = "payment.validation.proof_content_type_shape";
}
