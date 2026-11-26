using Tooba.BuildingBlocks.Results;
using Tooba.Pricing.Contracts.Seller;

namespace Tooba.Pricing.Contracts.Seller;

/// <summary>Pricing-owned seller price write request.</summary>
public sealed record SetSellerOfferPrice(
    Guid OfferId,
    Guid SellerPartyId,
    decimal Amount,
    string? Currency,
    string? Market,
    string? ExpectedRevision = null,
    Guid? ActorUserId = null,
    Guid OperationId = default);

/// <summary>Committed owner receipt; revision and facts are from the same Pricing transaction.</summary>
public sealed record SellerPriceWriteReceipt(
    Guid PriceId,
    Guid OfferId,
    string Revision,
    Guid OperationId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt,
    decimal? OldAmount,
    decimal NewAmount,
    string Currency);

/// <summary>Pricing-owned boundary for seller offer pricing.</summary>
public interface ISellerOfferPricingGateway
{
    /// <summary>Atomically creates/updates the active base price and its durable change fact.</summary>
    Task<Result<SellerPriceWriteReceipt>> SetPriceAsync(SetSellerOfferPrice request, CancellationToken cancellationToken);
}
