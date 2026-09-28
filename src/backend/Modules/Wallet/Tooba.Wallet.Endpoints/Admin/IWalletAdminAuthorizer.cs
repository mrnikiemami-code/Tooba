using Microsoft.AspNetCore.Http;

namespace Tooba.Wallet.Endpoints.Admin;

/// <summary>Neutral admin auth seam for Wallet Endpoints (Host implements).</summary>
public interface IWalletAdminAuthorizer
{
    /// <summary>
    /// Requires admin panel authorization plus the named Wallet/GiftCard capability.
    /// Authorization-service unavailability fails closed (503).
    /// </summary>
    Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken);
}
