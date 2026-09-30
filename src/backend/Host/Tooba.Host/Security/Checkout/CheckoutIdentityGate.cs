using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Catalog.Contracts.Checkout;

namespace Tooba.Host.Security.Checkout;

/// <summary>
/// Host enforcement gate: reads Catalog-owned policy via Contracts and enforces session for AuthenticatedOnly.
/// </summary>
public sealed class CheckoutIdentityGate
{
    private readonly ICatalogCheckoutIdentityPolicyLookup _policyLookup;
    private readonly CurrentAuthenticatedSession _session;

    /// <summary>Creates the gate over Catalog policy lookup + current session.</summary>
    internal CheckoutIdentityGate(
        ICatalogCheckoutIdentityPolicyLookup policyLookup,
        CurrentAuthenticatedSession session)
    {
        _policyLookup = policyLookup;
        _session = session;
    }

    /// <summary>Effective policy name; missing Catalog row yields AuthenticatedOnly.</summary>
    public async Task<string> GetEffectivePolicyNameAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _policyLookup.GetEffectiveAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(snapshot.Policy)
            ? "AuthenticatedOnly"
            : snapshot.Policy;
    }

    /// <summary>True when policy is GuestAllowed (case-insensitive).</summary>
    public async Task<bool> IsGuestAllowedAsync(CancellationToken cancellationToken)
    {
        var policy = await GetEffectivePolicyNameAsync(cancellationToken);
        return string.Equals(policy, "GuestAllowed", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>In AuthenticatedOnly without customer session, throws checkout.authentication_required.</summary>
    public async Task EnsureCheckoutActorAsync(CancellationToken cancellationToken)
    {
        if (await IsGuestAllowedAsync(cancellationToken))
        {
            return;
        }

        if (_session.IsAuthenticated && _session.UserId is Guid userId && userId != Guid.Empty)
        {
            return;
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.CheckoutAuthenticationRequired));
    }
}
