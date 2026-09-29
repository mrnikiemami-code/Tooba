using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>Host transport adapter — seller Actor + SellerPartyId for Order seller routes.</summary>
public sealed class HostOrderSellerAuthorizer(ISellerPanelAccess sellerAccess) : IOrderSellerAuthorizer
{
    /// <inheritdoc />
    public async Task<(Guid ActorUserId, Guid SellerPartyId, SemanticError? Error)> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        try
        {
            var (actor, seller) = await sellerAccess.RequireAuthorizedAsync(
                httpContext.Request, cancellationToken);
            return (actor, seller, null);
        }
        catch (PlatformHttpException ex)
        {
            return (Guid.Empty, Guid.Empty, new SemanticError(ex.ErrorCode ?? SellerSecurityErrorCodes.ActorMissing));
        }
    }
}
