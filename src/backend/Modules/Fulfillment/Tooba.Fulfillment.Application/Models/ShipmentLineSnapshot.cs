using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Models;


/// <summary>
/// snapshot خط محموله.
/// </summary>
public sealed record ShipmentLineSnapshot(Guid OrderLineId, decimal Quantity);
