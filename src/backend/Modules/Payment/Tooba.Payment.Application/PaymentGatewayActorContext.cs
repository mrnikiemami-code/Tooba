namespace Tooba.Payment.Application;

/// <summary>
/// زمینهٔ scoped هویت خریدار برای Initiate درگاه‌هایی مثل wallet که Actor را در قرارداد پایه ندارند.
/// </summary>
public sealed class PaymentGatewayActorContext
{
    /// <summary>Actor جاری فرمان Initiate؛ Guid.Empty اگر تنظیم نشده.</summary>
    public Guid ActorUserId { get; set; }
}
