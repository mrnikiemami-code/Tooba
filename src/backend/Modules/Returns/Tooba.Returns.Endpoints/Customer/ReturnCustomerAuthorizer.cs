using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Returns.Endpoints.Customer;

/// <summary>
/// Module-owned customer Actor resolver for Returns customer routes.
/// Uses the platform <see cref="ICurrentAuthenticatedUser"/> seam; no Host dependency.
/// Preserves Host semantics: authenticated or Dev/Testing header; no guest fallback.
/// </summary>
public sealed class ReturnCustomerAuthorizer(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IReturnCustomerAuthorizer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Guid? TryResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (currentUser.IsAuthenticated && currentUser.UserId is { } authenticated)
            return authenticated;

        if ((environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            && httpContext.Request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return null;
    }
}
