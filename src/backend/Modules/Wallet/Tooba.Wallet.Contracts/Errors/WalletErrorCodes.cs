namespace Tooba.Wallet.Contracts.Errors;

/// <summary>
/// Stable Wallet semantic error codes owned by the module for HTTP/use-case outcomes and domain
/// invariants.
/// <para>
/// Identity is the code itself — never a message string and never localized prose. The values are the
/// machine codes emitted by the Wallet Domain/Directory/Application and mapped by the composed error
/// catalog; they must never be renamed or repurposed.
/// </para>
/// <para>
/// This is the single canonical home for Wallet stable-code identity. Ownership is unique: the
/// <c>wallet.</c> and <c>giftcard.</c> keyspaces are Wallet-owned, registered exactly once by
/// <c>WalletErrorCatalogContributor</c> and localized by <c>WalletErrorResourceSet</c>. The module
/// never re-registers a foreign-owned descriptor and no other module registers a Wallet code.
/// </para>
/// <para>
/// The surface is split by reachability so the AMSC gates stay honest:
/// <list type="bullet">
/// <item><see cref="IsHttpReachable"/> — codes an HTTP client can observe; each has exactly one
/// descriptor in <c>WalletErrorCatalogContributor</c>.</item>
/// <item><see cref="IsDomainInvariant"/> — codes the Domain aggregates and the Infrastructure
/// directory throw as typed faults. They are the <c>Result</c>-oriented identity of a rejected
/// invariant and are localized, but they never get a dedicated HTTP descriptor: the owning use case
/// maps them onto the stable public outcome code of that operation, so no client-visible code is
/// added and the existing response shape is preserved byte-for-byte.</item>
/// </list>
/// <see cref="IsKnown"/> is the union of both sets and is exactly what <c>WalletOperation</c> filters
/// on; a Wallet fault therefore maps to <c>Result</c>, while a code owned by another module (or an
/// unexpected fault) propagates untouched to the canonical global exception boundary. Classification
/// is by typed code only — never by message text.
/// </para>
/// <para>
/// <c>customer.session.required</c> is a shared Foundation-owned cross-cutting code: its descriptor
/// and both-culture resources belong to <c>FoundationErrorCatalogContributor</c>. The Wallet HTTP
/// boundary consumes <c>FoundationErrorCodes.CustomerSessionRequired</c> directly, so it is
/// deliberately absent from this class.
/// </para>
/// </summary>
public static class WalletErrorCodes
{
    private static readonly HashSet<string> HttpReachableCodes = new(StringComparer.Ordinal)
    {
        WalletRejected,
        RedeemRejected,
        GiftCardRejected,
        GiftCardIssueRejected,
        GiftCardRevokeRejected,
        GiftCardMissing,
        WalletMissing,
        AdjustRejected,
        AuthorizationUnavailable,
        DemoNotReady,
    };

    private static readonly HashSet<string> DomainInvariantCodes = new(StringComparer.Ordinal)
    {
        // Account / currency / shared identity invariants.
        AccountIdsRequired,
        IdsRequired,
        CurrencyRequired,
        CurrencyInvalid,
        CurrencyMismatch,
        AccountNotMutable,
        AccountNotFound,
        AmountPositive,

        // Idempotency and metadata invariants.
        IdempotencyRequired,
        IdempotencyInvalid,
        IdempotencyConflict,
        MetadataTooLong,

        // Gift-card aggregate invariants.
        GiftCardIdsRequired,
        GiftCardAmountPositive,
        GiftCardIssuerRequired,
        GiftCardExpiryFuture,
        GiftCardAmountsInvalid,
        GiftCardNotRevocable,
        GiftCardRedeemAmountInvalid,
        GiftCardRevoked,
        GiftCardFullyRedeemed,
        GiftCardExpired,
        GiftCardStatusInvalid,
        GiftCardZeroRemaining,
        GiftCardCodeRequired,
        GiftCardCodeLength,
        GiftCardCodeNotFound,
        GiftCardNotFound,
        GiftCardStatusParse,

        // Gift-card redemption invariants.
        GiftCardRedemptionIds,
        GiftCardRedemptionAmount,
        GiftCardRedemptionId,
        RedemptionOwnerMismatch,

        // Ledger invariants.
        LedgerIdsRequired,
        LedgerAmountPositive,
        LedgerSourceTypeInvalid,

        // Admin adjustment invariants.
        AdjustmentDirectionInvalid,
        AdjustmentReasonInvalid,
        BalanceInsufficient,

        // Platform-side (outbox translation) invariant.
        OutboxUnmappedEventType,
    };
    public static IReadOnlyCollection<string> HttpReachable => HttpReachableCodes;

    /// <summary>
    /// Exactly the Domain/Directory invariant codes that build a typed Wallet fault and are mapped
    /// onto the stable public outcome code, without a dedicated HTTP descriptor.
    /// </summary>
    public static IReadOnlyCollection<string> DomainInvariants => DomainInvariantCodes;

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed
    /// to surface as a use-case fault. Used by <c>WalletOperation</c> so Wallet faults map to
    /// <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched
    /// to the canonical global exception boundary. Classification is by typed code only — never by
    /// message text.
    /// </summary>
    /// <param name="code">Candidate machine code.</param>
    /// <returns>True when the code belongs to the Wallet known-code surface.</returns>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && (HttpReachableCodes.Contains(code) || DomainInvariantCodes.Contains(code));

    /// <summary>True when the code is a client-observable, catalogued outcome code.</summary>
    /// <param name="code">Candidate machine code.</param>
    /// <returns>True when the code is in <see cref="HttpReachable"/>.</returns>
    public static bool IsHttpReachable(string? code) =>
        !string.IsNullOrWhiteSpace(code) && HttpReachableCodes.Contains(code);

    /// <summary>True when the code is a Domain/Directory invariant without a dedicated HTTP descriptor.</summary>
    /// <param name="code">Candidate machine code.</param>
    /// <returns>True when the code is in <see cref="DomainInvariants"/>.</returns>
    public static bool IsDomainInvariant(string? code) =>
        !string.IsNullOrWhiteSpace(code) && DomainInvariantCodes.Contains(code);

    // --- client-observable HTTP outcomes (one catalog descriptor each) ---------------------------

    /// <summary>Wallet summary/ledger request rejected (400).</summary>
    public const string WalletRejected = "wallet.rejected";

    /// <summary>Gift-card redemption rejected (400).</summary>
    public const string RedeemRejected = "wallet.redeem.rejected";

    /// <summary>Admin gift-card listing rejected (400).</summary>
    public const string GiftCardRejected = "giftcard.rejected";

    /// <summary>Admin gift-card issue rejected (400).</summary>
    public const string GiftCardIssueRejected = "giftcard.issue.rejected";

    /// <summary>Admin gift-card revoke rejected (400).</summary>
    public const string GiftCardRevokeRejected = "giftcard.revoke.rejected";

    /// <summary>Admin gift card not found (404).</summary>
    public const string GiftCardMissing = "giftcard.missing";

    /// <summary>Admin wallet inspection target not found (404).</summary>
    public const string WalletMissing = "wallet.missing";

    /// <summary>Admin wallet adjustment rejected (400).</summary>
    public const string AdjustRejected = "wallet.adjust.rejected";

    /// <summary>
    /// Authorization service unavailable; the admin capability must fail closed (503).
    /// The canonical Host-facing authority for this admin-auth code is
    /// <c>Tooba.Wallet.Endpoints.Admin.WalletAdminAuthorizationCodes.AuthorizationUnavailable</c>,
    /// colocated with <c>IWalletAdminAuthorizer</c> so Host never references the Application project.
    /// </summary>
    public const string AuthorizationUnavailable = "wallet.authorization.unavailable";

    /// <summary>Development demo seed not ready (503).</summary>
    public const string DemoNotReady = "wallet.demo.not_ready";

    // --- Domain/Directory invariants (typed fault; no dedicated HTTP descriptor) -----------------

    /// <summary>Account id and owner actor id are required.</summary>
    public const string AccountIdsRequired = "wallet.account.ids_required";

    /// <summary>Seeded identity ids are required.</summary>
    public const string IdsRequired = "wallet.ids_required";

    /// <summary>Currency is required.</summary>
    public const string CurrencyRequired = "wallet.currency_required";

    /// <summary>Currency code length is invalid.</summary>
    public const string CurrencyInvalid = "wallet.currency_invalid";

    /// <summary>Ledger/account currency does not match the requested currency.</summary>
    public const string CurrencyMismatch = "wallet.currency_mismatch";

    /// <summary>Account is not open for ledger mutation.</summary>
    public const string AccountNotMutable = "wallet.account.not_mutable";

    /// <summary>The wallet account for the supplied customer actor was not found.</summary>
    public const string AccountNotFound = "wallet.account.not_found";

    /// <summary>Idempotency key is required.</summary>
    public const string IdempotencyRequired = "wallet.idempotency_required";

    /// <summary>Idempotency key is invalid (too long).</summary>
    public const string IdempotencyInvalid = "wallet.idempotency_invalid";

    /// <summary>Idempotency key conflicts with a different stored operation.</summary>
    public const string IdempotencyConflict = "wallet.idempotency_conflict";

    /// <summary>Ledger metadata JSON exceeds the allowed length.</summary>
    public const string MetadataTooLong = "wallet.metadata_too_long";

    /// <summary>Amount must be positive (shared id/idempotency/amount guard of the order-payment and refund-credit seams).</summary>
    public const string AmountPositive = "wallet.amount.positive";

    /// <summary>Gift-card id and issuer id are required.</summary>
    public const string GiftCardIdsRequired = "wallet.giftcard.ids_required";

    /// <summary>Gift-card initial amount must be positive.</summary>
    public const string GiftCardAmountPositive = "wallet.giftcard.amount_positive";

    /// <summary>Gift-card issuer actor is required.</summary>
    public const string GiftCardIssuerRequired = "wallet.giftcard.issuer_required";

    /// <summary>Gift-card expiry must be in the future.</summary>
    public const string GiftCardExpiryFuture = "wallet.giftcard.expiry_future";

    /// <summary>Seeded gift-card amounts are inconsistent.</summary>
    public const string GiftCardAmountsInvalid = "wallet.giftcard.amounts_invalid";

    /// <summary>Gift card cannot be revoked in its current status.</summary>
    public const string GiftCardNotRevocable = "wallet.giftcard.not_revocable";

    /// <summary>Redemption amount is invalid for the remaining balance.</summary>
    public const string GiftCardRedeemAmountInvalid = "wallet.giftcard.redeem_amount_invalid";

    /// <summary>Gift card is revoked.</summary>
    public const string GiftCardRevoked = "wallet.giftcard.revoked";

    /// <summary>Gift card is fully redeemed.</summary>
    public const string GiftCardFullyRedeemed = "wallet.giftcard.fully_redeemed";

    /// <summary>Gift card is expired.</summary>
    public const string GiftCardExpired = "wallet.giftcard.expired";

    /// <summary>Gift-card status is not redeemable.</summary>
    public const string GiftCardStatusInvalid = "wallet.giftcard.status_invalid";

    /// <summary>Gift card has no remaining balance.</summary>
    public const string GiftCardZeroRemaining = "wallet.giftcard.zero_remaining";

    /// <summary>Gift-card code is required.</summary>
    public const string GiftCardCodeRequired = "wallet.giftcard.code_required";

    /// <summary>Gift-card code length is invalid.</summary>
    public const string GiftCardCodeLength = "wallet.giftcard.code_length";

    /// <summary>No gift card exists for the supplied code.</summary>
    public const string GiftCardCodeNotFound = "wallet.giftcard.code_not_found";

    /// <summary>Gift card was not found.</summary>
    public const string GiftCardNotFound = "wallet.giftcard.not_found";

    /// <summary>Gift-card status filter value is not a known status.</summary>
    public const string GiftCardStatusParse = "wallet.giftcard.status_parse";

    /// <summary>Redemption, card and account ids are required.</summary>
    public const string GiftCardRedemptionIds = "wallet.giftcard.redemption_ids";

    /// <summary>Redemption amount must be positive.</summary>
    public const string GiftCardRedemptionAmount = "wallet.giftcard.redemption_amount";

    /// <summary>Seeded redemption id is required.</summary>
    public const string GiftCardRedemptionId = "wallet.giftcard.redemption_id";

    /// <summary>Idempotent redemption replay belongs to another wallet account.</summary>
    public const string RedemptionOwnerMismatch = "wallet.redemption.owner_mismatch";

    /// <summary>Ledger entry, account and source ids are required.</summary>
    public const string LedgerIdsRequired = "wallet.ledger.ids_required";

    /// <summary>Ledger amount must be positive.</summary>
    public const string LedgerAmountPositive = "wallet.ledger.amount_positive";

    /// <summary>Ledger source type is missing or too long.</summary>
    public const string LedgerSourceTypeInvalid = "wallet.ledger.source_type_invalid";

    /// <summary>Admin adjustment direction is not a known direction.</summary>
    public const string AdjustmentDirectionInvalid = "wallet.adjustment.direction_invalid";

    /// <summary>Admin adjustment reason is missing or exceeds the allowed length.</summary>
    public const string AdjustmentReasonInvalid = "wallet.adjustment.reason_invalid";

    /// <summary>Ledger balance is insufficient for the requested debit.</summary>
    public const string BalanceInsufficient = "wallet.balance.insufficient";

    /// <summary>External event emission from the Wallet outbox is not supported (platform-side).</summary>
    public const string OutboxUnmappedEventType = "wallet.outbox.unmapped_event_type";
}
