

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
}
