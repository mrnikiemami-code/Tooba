
namespace Tooba.Order.Domain;

/// <summary>قفل سطری مشتری برای جلوگیری از رقابت روی آخرین ظرفیت/سهمیه.</summary>
public sealed class CheckoutAbuseCustomerLock
{
    /// <summary>مشتری.</summary>
    public Guid CustomerId { get; init; }

    /// <summary>آخرین لمس داخل تراکنش.</summary>
    public DateTimeOffset TouchedAt { get; set; }
}
