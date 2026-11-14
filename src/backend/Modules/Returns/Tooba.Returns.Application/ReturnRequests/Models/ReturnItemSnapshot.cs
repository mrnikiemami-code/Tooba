using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;


/// <summary>
/// snapshot خط مرجوعی.
/// </summary>
public sealed record ReturnItemSnapshot(
    Guid ReturnItemId,
    Guid OrderLineId,
    decimal Quantity,
    decimal UnitPriceSnapshot,
    string Currency,
    Guid? ReservationId,
    ReturnItemCondition Condition = ReturnItemCondition.Usable,
    ReturnItemDisposition Disposition = ReturnItemDisposition.None,
    Guid? SourceShipmentId = null,
    Guid? SourceReceiptId = null);
