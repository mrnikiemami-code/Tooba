using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Models;


/// <summary>
/// عضویت فعال مرسوله در بسته تجمیعی.
/// </summary>
public sealed record ActivePackageMembershipSnapshot(
    Guid ShipmentId,
    Guid ConsolidatedPackageId,
    string PackageNumber,
    ConsolidatedPackageStatus PackageStatus);
