using Tooba.BuildingBlocks;

namespace Tooba.Payment.Infrastructure.Events;

/// <summary>
/// پرداخت ساخته شد. این رویداد سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار قرارداد.
    /// </summary>
    public const string EventTypeName = "payment.created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پرداخت داخلی؛ شمارهٔ درگاه نیست.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// checkout مرجع بدون FK دیتابیسی.
    /// </summary>
    public Guid CheckoutId { get; set; }
}

/// <summary>
/// شروع درگاه. موفقیت پرداخت نیست و سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentInitiatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار قرارداد.
    /// </summary>
    public const string EventTypeName = "payment.initiated.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// تلاش.
    /// </summary>
    public Guid AttemptId { get; set; }
}

/// <summary>
/// موفقیت فقط پس از Verify درگاه. متن callback این رویداد را نمی‌سازد.
/// </summary>
public sealed class PaymentSucceededIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار قرارداد.
    /// </summary>
    public const string EventTypeName = "payment.succeeded.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پرداخت تأییدشده.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// checkout برای تصویر سفارش.
    /// </summary>
    public Guid CheckoutId { get; set; }

    /// <summary>
    /// مبلغ تصویر؛ انتخاب مشتری نیست.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ارز تصویر سفارش.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// مرجع تراکنش تأییدشدهٔ درگاه.
    /// </summary>
    public string ProviderTransactionReference { get; set; } = string.Empty;
}

/// <summary>
/// شکست Verify. سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentFailedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار قرارداد.
    /// </summary>
    public const string EventTypeName = "payment.failed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// checkout مرجع.
    /// </summary>
    public Guid CheckoutId { get; set; }

    /// <summary>
    /// کد شکست درگاه در صورت وجود.
    /// </summary>
    public string? FailureCode { get; set; }
}
