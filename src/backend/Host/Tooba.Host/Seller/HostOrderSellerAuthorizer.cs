using Tooba.BuildingBlocks;
using Tooba.Order.Application.Seller;
using Tooba.Order.Endpoints.Seller;

namespace Tooba.Host.Seller;

/// <summary>Host transport adapter — seller Actor + SellerPartyId for Order seller routes.</summary>
public sealed class HostOrderSellerAuthorizer : IOrderSellerAuthorizer
{
    /// <inheritdoc />
    public async Task<(Guid ActorUserId, Guid SellerPartyId, SemanticError? Error)> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        try
        {
            var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
            var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
            var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var (actor, seller) = await SellerPanelAccess.RequireAuthorizedAsync(
                httpContext.Request,
                session,
                guard,
                environment,
                cancellationToken);
            return (actor, seller, null);
        }
        catch (PlatformHttpException ex)
        {
            return (Guid.Empty, Guid.Empty, new SemanticError(ex.ErrorCode ?? SellerOrderErrors.ActorMissing));
        }
    }
}
