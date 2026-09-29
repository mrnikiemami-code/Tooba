using Tooba.BuildingBlocks.Security;
using Tooba.Promotion.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>Host transport adapter for Promotion seller Endpoints auth.</summary>
public sealed class HostPromotionSellerAuthorizer(ISellerPanelAccess sellerAccess) : IPromotionSellerAuthorizer
{
    /// <inheritdoc />
    public async Task<Guid> RequireSellerPartyIdAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var (_, sellerPartyId) = await sellerAccess.RequireAuthorizedAsync(
            context.Request, cancellationToken);
        return sellerPartyId;
    }
}
