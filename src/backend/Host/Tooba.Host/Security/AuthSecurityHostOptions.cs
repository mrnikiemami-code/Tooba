using Microsoft.Extensions.Options;

namespace Tooba.Host;

/// <summary>
/// پیکربندی امنیت HTTP/احراز هویت Host.
/// </summary>
public sealed class AuthSecurityHostOptions
{
    /// <summary>
    /// مسیر پیکربندی: Tooba:AuthSecurity
    /// </summary>
    public const string SectionName = "Tooba:AuthSecurity";

    /// <summary>
    /// مبداهای CORS مجاز. خالی یعنی CORS غیرفعال (same-origin / proxy).
    /// </summary>
    public string[] CorsAllowedOrigins { get; set; } = [];

    /// <summary>
    /// هدرهای امنیتی پایه را فعال می‌کند.
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;

    /// <summary>
    /// HSTS فقط در Production و پشت HTTPS.
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// CSP گزارش-only برای Shopeiva؛ enforce نشده.
    /// </summary>
    public bool EnableCspReportOnly { get; set; } = true;

    /// <summary>
    /// سقف درخواست‌های auth-sensitive در هر پنجره برای هر IP+operation.
    /// </summary>
    public int AuthRateLimitPermitLimit { get; set; } = 30;

    /// <summary>
    /// طول پنجره محدودسازی auth به ثانیه.
    /// </summary>
    public int AuthRateLimitWindowSeconds { get; set; } = 60;

    /// <summary>
    /// حداکثر اندازه بدنه درخواست HTTP (بایت).
    /// </summary>
    public long MaxRequestBodyBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>
/// اعتبارسنج Production برای AuthSecurity.
/// </summary>
internal sealed class AuthSecurityOptionsValidator : IValidateOptions<AuthSecurityHostOptions>
{
    private readonly IHostEnvironment _environment;

    public AuthSecurityOptionsValidator(IHostEnvironment environment) => _environment = environment;

    public ValidateOptionsResult Validate(string? name, AuthSecurityHostOptions options)
    {
        if (options.AuthRateLimitPermitLimit <= 0)
        {
            return ValidateOptionsResult.Fail("Auth rate limit permit limit must be positive.");
        }

        if (options.AuthRateLimitWindowSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("Auth rate limit window must be positive.");
        }

        if (options.MaxRequestBodyBytes <= 0)
        {
            return ValidateOptionsResult.Fail("Max request body bytes must be positive.");
        }

        foreach (var origin in options.CorsAllowedOrigins)
        {
            if (string.Equals(origin.Trim(), "*", StringComparison.Ordinal))
            {
                return ValidateOptionsResult.Fail("CORS wildcard origin is forbidden.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
