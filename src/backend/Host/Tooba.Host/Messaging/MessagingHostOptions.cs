using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Tooba.Host;

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

/// <summary>
/// اعتبارسنجی پیکربندی messaging تا فرآیند با bus ناقص شروع نشود.
/// </summary>
internal sealed class MessagingOptionsValidator : IValidateOptions<MessagingHostOptions>
{
    private readonly IHostEnvironment? _environment;

    public MessagingOptionsValidator()
    {
    }

    public MessagingOptionsValidator(IHostEnvironment environment) => _environment = environment;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MessagingHostOptions options)
    {
        if (options.Enabled && options.UseInProcessTestDouble)
        {
            return ValidateOptionsResult.Fail(
                "Tooba:Messaging cannot enable SQL Transport and UseInProcessTestDouble together.");
        }

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (!string.IsNullOrWhiteSpace(options.Transport)
            && !options.Transport.Equals(MessagingHostOptions.CanonicalTransport, StringComparison.OrdinalIgnoreCase)
            && !options.Transport.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"Tooba:Messaging:Transport must be {MessagingHostOptions.CanonicalTransport}. RabbitMQ/AMQP is forbidden.");
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionReference))
        {
            return ValidateOptionsResult.Fail(
                "Tooba:Messaging:ConnectionReference is required when messaging is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.Schema)
            || options.Schema.Equals("catalog", StringComparison.OrdinalIgnoreCase)
            || options.Schema.Equals("identity", StringComparison.OrdinalIgnoreCase)
            || options.Schema.Equals("pricing", StringComparison.OrdinalIgnoreCase)
            || options.Schema.Equals("platform_probe", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                "Tooba:Messaging:Schema must be a dedicated infrastructure schema such as transport.");
        }

        if (_environment?.IsProduction() == true
            && string.IsNullOrWhiteSpace(options.ConnectionReference))
        {
            return ValidateOptionsResult.Fail(
                "Production requires Tooba:Messaging:ConnectionReference when messaging is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
