namespace Tooba.Order.Contracts.Shipping;

/// <summary>
/// TB-260: one accepted shipping group's durable <b>planned dispatch</b> (carrier handoff) plus the
/// customer receiving commitment it was accepted with. Read-only Order fact for operational queues.
/// </summary>
/// <remarks>
/// <para>
/// The values are the ones stored in the accepted evidence by TB-256 — the queue never recomputes a plan
/// from current carrier settings, never derives it from the customer date and never rewrites it.
/// </para>
/// <para>
/// The customer receiving date/window is a separate commitment and is carried explicitly so a consumer can
/// show «موعد ارسال» and «تحویل مشتری» as two rows instead of confusing them.
/// </para>
/// </remarks>
public sealed record OrderAcceptedGroupDispatchPlanFact(
    Guid SellerOrderId,
    Guid ShippingGroupId,
    Guid CheckoutId,
    Guid SellerPartyId,
    string MethodCode,
    string MethodLabel,
    DateOnly HandoffDate,
    long HandoffOpenTicks,
    long HandoffCloseTicks,
    string HandoffTimeZone,
    int TransportWorkingDays,
    int PolicyVersion,
    DateOnly? CustomerReceivingDate,
    string? CustomerReceivingTimeWindow);

/// <summary>
/// TB-260: Contracts-only lookup of durable dispatch plans owned by Order. Fulfillment never joins Order
/// storage and never reads a Shipping setting to answer "what is due now".
/// </summary>
public interface IOrderDispatchPlanFactsReader
{
    /// <summary>
    /// Every accepted group that carries a durable plan. Groups whose evidence has no plan (historical
    /// rows) are deliberately absent — that absence is what a consumer reports as «موعد ارسال مشخص نشده».
    /// </summary>
    Task<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> ListPlannedAsync(CancellationToken cancellationToken);

    /// <summary>Durable plans of the given seller orders only.</summary>
    Task<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> ListPlannedForSellerOrdersAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}
