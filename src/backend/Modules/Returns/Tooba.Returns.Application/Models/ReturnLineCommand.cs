using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// خط مرجوعی در فرمان.
/// </summary>
public sealed record ReturnLineCommand(Guid OrderLineId, decimal Quantity);
