using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Tooba.Host.Messaging;

/// <summary>
/// اعتبارسنجی پیکربندی messaging تا فرآیند با bus ناقص شروع نشود.
/// </summary>
internal sealed class MessagingOptionsValidator : IValidateOptions<MessagingHostOptions>
{
    public MessagingOptionsValidator()
    {
    }

    /// <summary>
    /// DI-compatible ctor. Production-empty ConnectionReference was redundant with the Enabled path check below.
    /// </summary>
    public MessagingOptionsValidator(IHostEnvironment environment)
    {
        _ = environment;
    }

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

        return ValidateOptionsResult.Success;
    }
}
