

namespace Tooba.Fulfillment.Domain.ValueObjects;


/// <summary>
/// وضعیت بسته تجمیعی چندفروشنده‌ای. لایهٔ ارکستراسیون است، نه جایگزین Shipment.
/// </summary>
public enum ConsolidatedPackageStatus
{
    /// <summary>ایجاد شده؛ قابل ابطال و ارسال مرکزی.</summary>
    Created = 0,

    /// <summary>تمام اعضای عضو ارسال شده‌اند.</summary>
    Dispatched = 1,

    /// <summary>تمام اعضای عضو تحویل شده‌اند.</summary>
    Delivered = 2,

    /// <summary>ابطال پیش از ارسال؛ عضویت آزاد شده.</summary>
    Cancelled = 3,
}
