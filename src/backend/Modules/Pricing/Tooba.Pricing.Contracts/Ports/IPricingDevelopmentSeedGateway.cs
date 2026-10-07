using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Pricing.Contracts.Ports;

/// <summary>Pricing-owned Development-support request for the base offer price.</summary>
public sealed record SetDevelopmentBasePrice(
    Guid OfferId,
    string Market,
    SalesChannel Channel,
    decimal Amount,
    string Currency,
    DateTimeOffset ValidFrom);

/// <summary>
/// Pricing-owned Development-support capability used by the Catalog attribute-schema seed
/// so Catalog can author and activate a base price without touching Pricing persistence.
/// </summary>
public interface IPricingDevelopmentSeedGateway
{
    /// <summary>Creates and activates the base price when absent, otherwise no-ops.</summary>
    Task<Result> EnsureDevelopmentBasePriceAsync(
        SetDevelopmentBasePrice request,
        CancellationToken cancellationToken);
}
