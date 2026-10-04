using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;


/// <summary>
/// انتخاب خط/تعداد برای عملیات seller-scoped.
/// </summary>
public sealed record FulfillmentSelectionCommand(Guid OrderLineId, decimal Quantity);
