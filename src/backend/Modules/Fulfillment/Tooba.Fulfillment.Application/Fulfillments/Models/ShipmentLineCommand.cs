using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;


/// <summary>
/// خط محموله در فرمان.
/// </summary>
public sealed record ShipmentLineCommand(Guid OrderLineId, decimal Quantity);
