using Tooba.BuildingBlocks.Results;

namespace Tooba.Inventory.Contracts.Seller;

/// <summary>Seller request to set explicit platform-fulfilled new-sale allocation.</summary>
public sealed record SetSellerOfferAllocation(
    Guid OfferId,
    Guid SellerPartyId,
    decimal Quantity,
    string? ExpectedRevision = null,
    Guid? ActorUserId = null,
    Guid OperationId = default);

/// <summary>Committed allocation receipt from the Inventory owner transaction.</summary>
public sealed record OfferAllocationReceipt(
    Guid OfferId,
    decimal Quantity,
    decimal Held,
    decimal Available,
    string Revision,
    Guid OperationId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt);

/// <summary>Owner boundary for platform-fulfilled offered quantity.</summary>
public interface ISellerOfferAllocationGateway
{
    /// <summary>Sets the explicit offered quantity atomically.</summary>
    Task<Result<OfferAllocationReceipt>> SetAllocationAsync(
        SetSellerOfferAllocation request,
        CancellationToken cancellationToken);
}
