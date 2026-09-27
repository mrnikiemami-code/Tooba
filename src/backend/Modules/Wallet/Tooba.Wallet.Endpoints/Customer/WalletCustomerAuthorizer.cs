using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Wallet.Endpoints.Customer;

/// <summary>
/// Module-owned customer Actor resolver for Wallet customer routes.
/// Uses the platform <see cref="ICurrentAuthenticatedUser"/> seam; no Host dependency.
/// Preserves Host semantics: authenticated or Development-only header; no guest fallback.
/// </summary>
public sealed class WalletCustomerAuthorizer(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IWalletCustomerAuthorizer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Guid? TryResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (currentUser.IsAuthenticated && currentUser.UserId is { } authenticated)
            return authenticated;

        if (environment.IsDevelopment()
            && httpContext.Request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var actor)
            && actor != Guid.Empty)
        {
            return actor;
        }

        return null;
    }
}
