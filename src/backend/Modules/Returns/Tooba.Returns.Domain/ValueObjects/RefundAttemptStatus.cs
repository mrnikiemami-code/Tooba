

namespace Tooba.Returns.Domain.ValueObjects;


/// <summary>
/// وضعیت تلاش refund. با PaymentStatus یکی نیست.
/// </summary>
public enum RefundAttemptStatus
{
    /// <summary>در انتظار پاسخ درگاه.</summary>
    Pending = 0,

    /// <summary>موفق.</summary>
    Succeeded = 1,

    /// <summary>شکست.</summary>
    Failed = 2,
}
