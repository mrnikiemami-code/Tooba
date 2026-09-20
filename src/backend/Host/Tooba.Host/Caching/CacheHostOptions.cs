using Microsoft.Extensions.Options;

namespace Tooba.Host;

/// <summary>
/// تنظیمات کش فرآیند از <c>Tooba:Cache</c>. ایزولاسیون Tenant با پیکربندی خاموش نمی‌شود.
/// </summary>
internal sealed class CacheHostOptions
{
    /// <summary>
    /// اگر false باشد ورود ذخیره نمی‌شود؛ miss همیشه به منبع حقیقت می‌رود. مرز Tenant در کلیدساز باقی می‌ماند.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// ارائه‌دهندهٔ فعلی. فقط <c>Memory</c> یا <c>None</c>. Redis در این فاز مجاز نیست.
    /// </summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>
    /// سقف تعداد ورود در حافظهٔ فرآیند؛ هر ورود Size=1 دارد. بین نمونه‌های Host به اشتراک گذاشته نمی‌شود.
    /// </summary>
    public long EntryCountLimit { get; set; } = 10_000;

    /// <summary>
    /// single-flight داخل فرآیند برای GetOrCreate. قفل توزیع‌شده برای Redis بعدی است.
    /// </summary>
    public bool StampedeProtection { get; set; } = true;
}

/// <summary>
/// Redis را رد می‌کند و سقف حافظه را کران‌دار نگه می‌دارد.
/// </summary>
internal sealed class CacheOptionsValidator : IValidateOptions<CacheHostOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CacheHostOptions options)
    {
        if (options.EntryCountLimit <= 0)
        {
            return ValidateOptionsResult.Fail("Tooba:Cache:EntryCountLimit must be positive.");
        }

        var provider = options.Provider?.Trim() ?? "";
        if (provider.Equals("Redis", StringComparison.OrdinalIgnoreCase)
            || provider.Contains("StackExchange", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("Redis cache is not enabled in this foundation; use Memory or None.");
        }

        if (!provider.Equals("Memory", StringComparison.OrdinalIgnoreCase)
            && !provider.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("Tooba:Cache:Provider must be Memory or None.");
        }

        if (!options.Enabled && provider.Equals("Memory", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Success;
    }
}
