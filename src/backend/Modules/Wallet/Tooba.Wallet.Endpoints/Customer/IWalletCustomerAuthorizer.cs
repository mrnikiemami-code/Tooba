using Microsoft.AspNetCore.Http;

namespace Tooba.Wallet.Endpoints.Customer;

/// <summary>Neutral customer auth seam for Wallet Endpoints (Host implements).</summary>
public interface IWalletCustomerAuthorizer
{
    /// <summary>Resolves customer actor user id, or null when unauthenticated.</summary>
    Guid? TryResolveActor(HttpContext httpContext);
}
