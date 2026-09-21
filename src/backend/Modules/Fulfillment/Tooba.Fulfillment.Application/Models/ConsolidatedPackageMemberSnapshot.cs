using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Models;


/// <summary>
/// عضو بسته تجمیعی در snapshot.
/// </summary>
public sealed record ConsolidatedPackageMemberSnapshot(
    Guid ConsolidatedPackageMemberId,
    Guid ShipmentId,
    Guid SellerPartyId,
    Guid FulfillmentId,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ReleasedAt);
