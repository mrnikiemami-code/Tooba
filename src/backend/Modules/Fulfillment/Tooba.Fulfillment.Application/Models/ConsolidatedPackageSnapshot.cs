using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Models;


/// <summary>
/// snapshot بسته تجمیعی.
/// </summary>
public sealed record ConsolidatedPackageSnapshot(
    Guid ConsolidatedPackageId,
    string PackageNumber,
    Guid CheckoutId,
    ConsolidatedPackageStatus Status,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string? TrackingReference,
    string? Note,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<ConsolidatedPackageMemberSnapshot> Members);
