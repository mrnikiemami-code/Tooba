using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// eligibility خط مرجوعی برای یک OrderLine.
/// </summary>
public sealed record ReturnLineEligibility(
    Guid OrderLineId,
    decimal DeliveredQuantity,
    decimal AlreadyReturnedQuantity,
    decimal RemainingReturnableQuantity);
