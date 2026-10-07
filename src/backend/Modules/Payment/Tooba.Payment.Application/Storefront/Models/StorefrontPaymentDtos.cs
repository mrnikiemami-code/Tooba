namespace Tooba.Payment.Application.Storefront.Models;

/// <summary>Stable payment provider codes used by storefront orchestration (no Infrastructure leak).</summary>
public static class PaymentProviderCodes
{
    public const string Wallet = "wallet";
    public const string Manual = "manual";
    public const string GatewayCatalog = "gateway";

    public static bool IsManual(string? providerCode) =>
        string.Equals(providerCode, Manual, StringComparison.OrdinalIgnoreCase);

    public static bool IsWallet(string? providerCode) =>
        string.Equals(providerCode, Wallet, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Guest actor id aligned with storefront checkout guest identity.</summary>
public static class PaymentStorefrontActors
{
    public static readonly Guid GuestActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000009");
}

public sealed record StorefrontWalletQuoteDto(
    Guid CheckoutId,
    decimal WalletBalance,
    decimal MaxUsable,
    decimal RemainingPayable,
    bool CanPayFullyWithWallet,
    string Currency,
    bool MixedTenderDeferred,
    bool ManualCardToCardEnabled = false);

public sealed record StorefrontPaymentMethodOptionDto(
    string Code,
    string LabelFa,
    string DescriptionFa);

public sealed record StorefrontPaymentMethodsDto(
    IReadOnlyList<StorefrontPaymentMethodOptionDto> Methods,
    bool ManualCardToCardEnabled,
    string ManualProofRequirement = "Optional",
    string ManualPaymentInstructions = "");

public sealed record StorefrontPaymentInitiationDto(
    Guid PaymentId,
    Guid AttemptId,
    Guid CheckoutId,
    string Status,
    string ProviderCode,
    string ProviderRequestReference,
    string RedirectUrl,
    decimal Amount,
    string Currency,
    bool RequiresPspRedirect = true);

public sealed record StorefrontPaymentAllocationDto(
    Guid SellerOrderId,
    decimal AllocatedAmount,
    string Currency,
    string TargetKind);

public sealed record StorefrontManualEvidenceHistoryDto(
    Guid AttemptId,
    string AttemptStatus,
    string? CustomerTransferReference,
    Guid? ProofMediaAssetId,
    DateTimeOffset? EvidenceSubmittedAt,
    string? FailureCode);

public sealed record StorefrontPaymentDto(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    string Status,
    string ProviderCode,
    IReadOnlyList<StorefrontPaymentAllocationDto> Allocations,
    string? CustomerTransferReference = null,
    Guid? ProofMediaAssetId = null,
    DateTimeOffset? EvidenceSubmittedAt = null,
    string? OrderNumber = null,
    string ManualProofRequirement = "Optional",
    string ManualPaymentInstructions = "",
    bool CanSubmitManualEvidence = false,
    bool CanRetryManual = false,
    IReadOnlyList<StorefrontManualEvidenceHistoryDto>? EvidenceHistory = null,
    bool CanRetryUnpaid = false);

public sealed record StorefrontSandboxContextDto(
    Guid PaymentId,
    Guid CheckoutId,
    string StoreName,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string ProviderLabel,
    bool Sandbox);
