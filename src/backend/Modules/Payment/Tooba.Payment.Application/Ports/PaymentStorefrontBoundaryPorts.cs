namespace Tooba.Payment.Application.Ports;

public interface ICheckoutActorPolicyPort
{
    Task EnsureCheckoutActorAsync(CancellationToken cancellationToken);
}

/// <summary>Gateway catalog / sandbox / manual policy (Infrastructure-backed).</summary>
public interface IPaymentGatewayCatalogPort
{
    string DefaultProvider { get; }
    bool ManualCardToCardEnabled { get; }
    string ManualProofRequirement { get; }
    string ManualPaymentInstructions { get; }
    string StoreDisplayName { get; }
    int ManualPaymentReviewHoldHours { get; }
    bool IsOnlineGatewayOffered();
    bool IsSandboxSimulatorEnabled();
    void MarkSandboxDecline(string providerRequestReference);
}

/// <summary>Webhook signature verification without leaking Infrastructure options into Endpoints.</summary>
public interface IPaymentWebhookSignatureVerifier
{
    string SignatureHeaderName { get; }
    bool TryValidate(ReadOnlySpan<byte> body, string? signatureHeader, out string errorCode);
}
