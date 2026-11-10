

namespace Tooba.Fulfillment.Domain.ValueObjects;


/// <summary>
/// وضعیت محموله. Order نیست.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>ایجاد شده.</summary>
    Created = 0,

    /// <summary>dispatch شده.</summary>
    Dispatched = 1,

    /// <summary>در مسیر.</summary>
    InTransit = 2,

    /// <summary>تحویل شده.</summary>
    Delivered = 3,

    /// <summary>شکست خورده.</summary>
    Failed = 4,

    /// <summary>لغو شده.</summary>
    Cancelled = 5,

    /// <summary>TB-258: courier recorded a non-delivery attempt after dispatch (not delivered, not cancelled).</summary>
    NonDelivered = 6,

    /// <summary>TB-258: the carrier is physically bringing the parcel back to origin.</summary>
    Returning = 7,

    /// <summary>TB-258: origin receipt confirmed; only physically usable received quantity becomes resendable.</summary>
    Returned = 8,
}
