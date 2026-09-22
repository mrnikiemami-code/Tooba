using Microsoft.AspNetCore.Http;

namespace Tooba.Payment.Endpoints.Storefront;

/// <summary>Resolves storefront payment actor (authenticated or guest).</summary>
public interface IPaymentStorefrontAuthorizer
{
    /// <summary>Authenticated user id when present; null means guest path.</summary>
    Guid? TryResolveAuthenticatedUserId(HttpContext httpContext);
}

/// <summary>Wire-only initiate body.</summary>
public sealed record InitiatePaymentBody(
    Guid CartId,
    string IdempotencyKey,
    bool UseWallet = false,
    string? ProviderCode = null)
{
    public bool WantsWallet =>
        UseWallet
        || string.Equals(ProviderCode, "wallet", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Wire-only sandbox complete body.</summary>
public sealed record SandboxCompleteBody(
    Guid CartId,
    Guid AttemptId,
    string ProviderRequestReference,
    string Outcome);

/// <summary>Wire-only manual evidence body.</summary>
public sealed record ManualEvidenceBody(
    Guid CartId,
    string TransferReference,
    Guid? ProofMediaAssetId);

/// <summary>Wire-only cart id body.</summary>
public sealed record PaymentCartBody(Guid CartId);
