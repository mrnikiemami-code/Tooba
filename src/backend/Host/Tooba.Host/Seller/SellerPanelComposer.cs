using Tooba.Party.Application;

namespace Tooba.Host.Seller;

/// <summary>
/// Composes the thin seller dashboard shell (display name only) for the remaining Host seller routes.
/// Catalog variant listing moved to Catalog (R2); Order counts come from Order CQRS via endpoints.
/// </summary>
public sealed class SellerPanelComposer(
    IPartyLookupGateway parties)
{
    /// <summary>
    /// Builds seller display shell for dashboard. Order counts come from Order CQRS via endpoints.
    /// </summary>
    public async Task<(string DisplayName, bool SellerFound)> GetSellerDisplayAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var seller = await parties.FindByIdAsync(sellerPartyId, cancellationToken);
        if (seller is null)
        {
            return (string.Empty, false);
        }

        return (seller.DisplayName, true);
    }
}
