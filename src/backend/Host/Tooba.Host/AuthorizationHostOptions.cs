using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// پیکربندی مجوز. توکن در مخزن نیست و لاگ نمی‌شود.
/// </summary>
public sealed class AuthorizationHostOptions
{
    /// <summary>
    /// Disabled = همهٔ checkها Unavailable (fail-closed). InMemory فقط تست/توسعه. SpiceDb = adapter واقعی.
    /// </summary>
    public string Mode { get; set; } = "Disabled";

    /// <summary>
    /// اعمال schema فقط وقتی صریحاً true باشد؛ استارت تولید بازنویسی کور ندارد.
    /// </summary>
    public bool ApplySchemaOnStartup { get; set; }

    /// <summary>
    /// تنظیمات اتصال SpiceDB.
    /// </summary>
    public SpiceDbHostOptions SpiceDb { get; set; } = new();
}

/// <summary>
/// اتصال SpiceDB بدون secret در git.
/// </summary>
public sealed class SpiceDbHostOptions
{
    /// <summary>
    /// آدرس gRPC/HTTP؛ خالی یعنی پیکربندی ناقص.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// ارجاع credential یا مقدار env؛ در لاگ نباید بیاید.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// TLS الزامی برای مسیر غیرلوکال.
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// مهلت درخواست به ثانیه.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// حداکثر تلاش برای خطای گذرای زیرساخت (بدون retry روی DENY).
    /// </summary>
    public int RetryMaxAttempts { get; set; } = 3;

    /// <summary>
    /// تاخیر پایه backoff میلی‌ثانیه بین retry.
    /// </summary>
    public int RetryBaseDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// FullyConsistent یا MinimizeLatency برای checkهای بدون ZedToken.
    /// </summary>
    public string ConsistencyMode { get; set; } = "FullyConsistent";

    /// <summary>
    /// probe سبک gRPC در readiness وقتی Mode=SpiceDb.
    /// </summary>
    public bool ReadinessProbeEnabled { get; set; } = true;
}

/// <summary>
/// اعتبارسنجی Mode در Production: InMemory و allow-all ممنوع است.
/// </summary>
internal sealed class AuthorizationOptionsValidator : IValidateOptions<AuthorizationHostOptions>
{
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// اعتبارسنج را با محیط Host می‌سازد.
    /// </summary>
    public AuthorizationOptionsValidator(IHostEnvironment environment) => _environment = environment;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AuthorizationHostOptions options)
    {
        var mode = options.Mode.Trim();
        if (mode is not ("Disabled" or "InMemory" or "SpiceDb"))
        {
            return ValidateOptionsResult.Fail("Tooba:Authorization:Mode must be Disabled, InMemory, or SpiceDb.");
        }

        if (_environment.IsProduction() && mode == "InMemory")
        {
            return ValidateOptionsResult.Fail("InMemory authorization is not allowed in Production.");
        }

        if (mode == "SpiceDb")
        {
            if (string.IsNullOrWhiteSpace(options.SpiceDb.Endpoint))
            {
                return ValidateOptionsResult.Fail("SpiceDB endpoint is required when Mode=SpiceDb.");
            }

            if (string.IsNullOrWhiteSpace(options.SpiceDb.Token))
            {
                return ValidateOptionsResult.Fail("SpiceDB token is required when Mode=SpiceDb.");
            }

            if (options.SpiceDb.TimeoutSeconds <= 0)
            {
                return ValidateOptionsResult.Fail("SpiceDB timeout must be positive.");
            }

            if (options.SpiceDb.RetryMaxAttempts <= 0)
            {
                return ValidateOptionsResult.Fail("SpiceDB retry max attempts must be positive.");
            }

            if (options.SpiceDb.RetryBaseDelayMilliseconds < 0)
            {
                return ValidateOptionsResult.Fail("SpiceDB retry base delay cannot be negative.");
            }

            var consistency = options.SpiceDb.ConsistencyMode.Trim();
            if (consistency is not ("FullyConsistent" or "MinimizeLatency"))
            {
                return ValidateOptionsResult.Fail("SpiceDB consistency mode must be FullyConsistent or MinimizeLatency.");
            }

            if (_environment.IsProduction() && !options.SpiceDb.UseTls)
            {
                return ValidateOptionsResult.Fail("SpiceDB TLS must be enabled in Production.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
