namespace Tooba.Returns.Domain.ValueObjects;

/// <summary>
/// TB-265: who physically originated this return case.
/// <para>
/// Existing rows keep <see cref="CustomerDelivered"/> semantics: the customer received the goods and returns them
/// through the delivered-customer RMA window. <see cref="CourierOrigin"/> is a parcel that came back to the seller
/// from the courier and was physically received at origin — it is NOT a never-delivered customer RMA and must never
/// be admitted by faking a delivery.
/// </para>
/// </summary>
public enum ReturnOrigin
{
    /// <summary>Customer-delivered RMA (the historical, unchanged path).</summary>
    CustomerDelivered = 0,

    /// <summary>Courier-origin receipt: goods physically back at the seller from a returned parcel.</summary>
    CourierOrigin = 1,
}

/// <summary>
/// TB-265: physical condition of the received quantity of one returned line.
/// Only <see cref="Usable"/> may become saleable stock again; <see cref="Damaged"/> is never restocked as sellable.
/// </summary>
public enum ReturnItemCondition
{
    /// <summary>Received and physically usable.</summary>
    Usable = 0,

    /// <summary>Received but damaged / not resellable.</summary>
    Damaged = 1,
}

/// <summary>
/// TB-265: deterministic disposition of one received line, recorded so a damaged unit never silently disappears and
/// a healthy unit is never counted twice.
/// </summary>
public enum ReturnItemDisposition
{
    /// <summary>No physical disposition recorded yet (customer-delivered RMA legacy rows).</summary>
    None = 0,

    /// <summary>Healthy unit that may return to saleable stock once, through the owning Inventory return policy.</summary>
    SaleableRestock = 1,

    /// <summary>Damaged unit: explicitly non-saleable, retained as a durable disposition fact.</summary>
    DamagedNonSaleable = 2,
}

/// <summary>
/// TB-265: durable progress of the courier-origin approval closure. Each phase is recorded so a retry after any
/// boundary resumes the exact remaining work instead of repeating money/stock effects.
/// </summary>
public enum CourierOriginClosurePhase
{
    /// <summary>Not a courier-origin case, or nothing closed yet.</summary>
    None = 0,

    /// <summary>The owning order-line delivery obligation has been closed.</summary>
    ObligationClosed = 1,

    /// <summary>The source branch is settled and its unconsumed held quantity released once.</summary>
    StockSettled = 2,

    /// <summary>The financial refund may proceed / has proceeded for this case.</summary>
    RefundReleased = 3,
}
