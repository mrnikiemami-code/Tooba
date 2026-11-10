using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;


/// <summary>TB-258: one physically received line of a return receipt submitted to the owning directory.</summary>
public sealed record ReturnReceiptLineCommand(
    Guid OrderLineId,
    decimal Quantity,
    ReturnReceiptCondition Condition = ReturnReceiptCondition.Usable);
