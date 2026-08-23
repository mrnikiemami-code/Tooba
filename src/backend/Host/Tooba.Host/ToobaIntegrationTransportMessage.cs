namespace Tooba.Host;

/// <summary>
/// پاکت transport پایدار Tooba. قرارداد کسب‌وکار ماژول نیست و جایگزین Domain Event نمی‌شود.
/// TenantId از این پاکت و header می‌آید نه از Host.
/// </summary>
public sealed class ToobaIntegrationTransportMessage
{
    /// <summary>
    /// نام قراردادی type map؛ AssemblyQualifiedName نیست.
    /// </summary>
    public string EventType { get; init; } = "";

    /// <summary>
    /// نسخهٔ قرارداد Integration.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// شناسهٔ پایدار رویداد؛ با Outbox Id یکی است.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// زمان وقوع UTC؛ ترتیب سراسری تضمین نیست.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Tenant پایدار یا تهی برای Marketplace.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Edition ذخیره‌شده در زمان persist.
    /// </summary>
    public string Edition { get; init; } = "";

    /// <summary>
    /// برچسب استقرار؛ هویت Tenant نیست.
    /// </summary>
    public string DeploymentId { get; init; } = "";

    /// <summary>
    /// همبستگی تله‌متری اختیاری.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// JSON فیلدهای کسب‌وکار بدون $type. هرگز لاگ نشود.
    /// </summary>
    public string PayloadJson { get; init; } = "";
}
