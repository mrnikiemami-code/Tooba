using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Models;


/// <summary>
/// خط محموله در فرمان.
/// </summary>
public sealed record ShipmentLineCommand(Guid OrderLineId, decimal Quantity);
