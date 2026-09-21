

namespace Tooba.Fulfillment.Domain.ValueObjects;


/// <summary>
/// وضعیت واحد fulfillment. با وضعیت تجاری Order یکی نیست.
/// </summary>
public enum FulfillmentStatus
{
    /// <summary>پس از Paid و آماده عملیات.</summary>
    ReadyToFulfill = 0,

    /// <summary>در حال پردازش انبار.</summary>
    Processing = 1,

    /// <summary>بسته‌بندی شده.</summary>
    Packed = 2,

    /// <summary>حداقل یک محموله dispatch شده.</summary>
    Dispatched = 3,

    /// <summary>در مسیر تحویل.</summary>
    InTransit = 4,

    /// <summary>تحویل نهایی.</summary>
    Delivered = 5,

    /// <summary>شکست عملیاتی.</summary>
    Failed = 6,

    /// <summary>لغو شده.</summary>
    Cancelled = 7,
}
