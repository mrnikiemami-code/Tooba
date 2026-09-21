using Tooba.BuildingBlocks.Results;

namespace Tooba.Pricing.Contracts;

/// <summary>Pricing-owned seller price write request.</summary>
public sealed record SetSellerOfferPrice(
    Guid OfferId,
    Guid SellerPartyId,
    decimal Amount,
    string? Currency,
    string? Market);

/// <summary>Pricing-owned boundary for seller offer pricing.</summary>
public interface ISellerOfferPricingGateway
{
    /// <summary>Creates or updates the active base price after seller ownership validation.</summary>
    Task<Result> SetPriceAsync(SetSellerOfferPrice request, CancellationToken cancellationToken);
}
