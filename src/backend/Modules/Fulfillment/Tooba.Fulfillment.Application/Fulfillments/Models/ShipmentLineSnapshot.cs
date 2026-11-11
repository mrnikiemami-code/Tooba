using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;


/// <summary>
/// snapshot خط محموله.
/// </summary>
/// <param name="OrderLineId">خط سفارش.</param>
/// <param name="Quantity">تخصیص‌یافته در این محموله.</param>
/// <param name="ResendableQuantity">
/// TB-258-R1: usable capacity physically received back on THIS parcel for this line, minus the resend parcels
/// already sourced from it. Zero for a parcel that never came back.
/// </param>
public sealed record ShipmentLineSnapshot(
    Guid OrderLineId,
    decimal Quantity,
    decimal ResendableQuantity = 0,
    /// <summary>TB-262: the damaged quantity physically received back on THIS parcel for this line.</summary>
    decimal DamagedQuantity = 0,
    /// <summary>TB-262: the damaged quantity of this parcel line that still awaits a durable decision.</summary>
    decimal OutstandingDamagedQuantity = 0);
