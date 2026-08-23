using Microsoft.Extensions.Options;

namespace Tooba.Host;

/// <summary>
/// تنظیمات transport پیام از بخش <c>Tooba:Messaging</c>. یک ConnectionReference در سطح استقرار است نه per-tenant.
/// </summary>
internal sealed class MessagingHostOptions
{
    /// <summary>
    /// اگر false باشد bus ساخته نمی‌شود؛ fallback خاموش به in-process رخ نمی‌دهد.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// کلید ConnectionReference پایگاه messaging استقرار؛ از Host یا TenantId مشتق نمی‌شود.
    /// </summary>
    public string ConnectionReference { get; set; } = "";

    /// <summary>
    /// schema زیرساخت SQL Transport؛ جدا از schemaهای کسب‌وکار ماژول.
    /// </summary>
    public string Schema { get; set; } = "transport";

    /// <summary>
    /// فقط در محیط Testing مجاز است. دابل in-process را به‌عنوان پیش‌فرض تولید روشن نمی‌کند.
    /// </summary>
    public bool UseInProcessTestDouble { get; set; }
}

/// <summary>
/// اعتبارسنجی پیکربندی messaging تا فرآیند با bus ناقص شروع نشود.
/// </summary>
internal sealed class MessagingOptionsValidator : IValidateOptions<MessagingHostOptions>
{
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
