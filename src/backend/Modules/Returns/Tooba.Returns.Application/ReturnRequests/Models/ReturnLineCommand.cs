using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;


/// <summary>
/// خط مرجوعی در فرمان.
/// </summary>
public sealed record ReturnLineCommand(Guid OrderLineId, decimal Quantity);
