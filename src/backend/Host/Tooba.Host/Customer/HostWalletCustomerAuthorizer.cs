#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Wallet.Endpoints.Customer;

namespace Tooba.Host.Customer;

/// <summary>Host transport adapter for Wallet customer Endpoints auth.</summary>
public sealed class HostWalletCustomerAuthorizer : IWalletCustomerAuthorizer
{
    internal const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    public Guid? TryResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var request = httpContext.Request;

        if (session.IsAuthenticated && session.UserId is { } userId)
            return userId;

        if (environment.IsDevelopment()
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var actor)
            && actor != Guid.Empty)
        {
            return actor;
        }

        return null;
    }
}
