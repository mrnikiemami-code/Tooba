namespace Tooba.Host.Messaging;

/// <summary>
/// تنظیمات PostgreSQL SQL Transport از بخش <c>Tooba:Messaging</c>.
/// </summary>
internal sealed class MessagingHostOptions
{
    /// <summary>
    /// Canonical transport mode. Only PostgreSql is supported; RabbitMQ is forbidden.
    /// </summary>
    public const string CanonicalTransport = "PostgreSql";

    /// <summary>
    /// اگر false باشد bus ساخته نمی‌شود؛ fallback خاموش به in-process رخ نمی‌دهد.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// transport ثابت: PostgreSQL SQL Transport. مقادیر دیگر رد می‌شوند.
    /// </summary>
    public string Transport { get; set; } = CanonicalTransport;

    /// <summary>
    /// کلید ConnectionReference پایگاه messaging استقرار.
    /// </summary>
    public string ConnectionReference { get; set; } = "";

    /// <summary>
    /// schema زیرساخت SQL Transport؛ جدا از schemaهای کسب‌وکار ماژول.
    /// </summary>
    public string Schema { get; set; } = "transport";

    /// <summary>
    /// فقط در محیط Testing مجاز است.
    /// </summary>
    public bool UseInProcessTestDouble { get; set; }
}
