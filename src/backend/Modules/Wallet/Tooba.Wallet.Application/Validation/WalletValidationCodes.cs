namespace Tooba.Wallet.Application.Validation;

/// <summary>
/// Stable transport-shape validation codes for Wallet.
/// <para>
/// These codes are transport identity: they are never localized and never encode business meaning.
/// They are deliberately not registered as catalog descriptors: the canonical
/// <c>ValidationBehavior</c> pipeline surfaces them inside the <c>validation.failed</c> envelope and
/// the per-property <c>validationErrors</c> map.
/// </para>
/// <para>
/// Only the transport shape is validated here; every business/domain rule (ownership, state
/// transition, real idempotency, record existence) stays in Application/Domain and is never
/// duplicated in a validator.
/// </para>
/// </summary>
public static class WalletValidationCodes
{
    /// <summary>Gift-card code is required on redemption.</summary>
    public const string CodeRequired = "wallet.validation.code_required";

    /// <summary>Gift-card code is outside the accepted length range.</summary>
    public const string CodeLength = "wallet.validation.code_length";

    /// <summary>Idempotency key exceeds the allowed length.</summary>
    public const string IdempotencyKeyTooLong = "wallet.validation.idempotency_key_too_long";

    /// <summary>Initial gift-card amount must be greater than zero.</summary>
    public const string InitialAmountPositive = "wallet.validation.initial_amount_positive";

    /// <summary>Currency code is outside the accepted length range.</summary>
    public const string CurrencyInvalid = "wallet.validation.currency_invalid";

    /// <summary>Gift-card status filter value is not a known status.</summary>
    public const string StatusInvalid = "wallet.validation.status_invalid";

    /// <summary>Admin list search expression exceeds the allowed length.</summary>
    public const string SearchTooLong = "wallet.validation.search_too_long";
}
