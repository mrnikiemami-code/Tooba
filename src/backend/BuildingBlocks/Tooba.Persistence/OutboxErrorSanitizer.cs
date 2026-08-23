using System.Text.RegularExpressions;

namespace Tooba.Persistence;

/// <summary>
/// بهداشتی‌سازی LastError برای Outbox: بدون secret، stack، connection string و payload.
/// </summary>
public static class OutboxErrorSanitizer
{
    private static readonly Regex Sensitive = new(
        "(password|pwd|secret|token|connectionstring|user id|username)\\s*=\\s*[^;\\s]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StackCue = new(
        @"\s+at\s+\S+\.\S+",
        RegexOptions.Compiled);

    /// <summary>
    /// حداکثر طول ستون last_error.
    /// </summary>
    public const int MaxLength = 256;

    /// <summary>
    /// نوع استثنا به‌علاوهٔ پیام کوتاه بهداشتی. <see cref="Exception.ToString"/> استفاده نمی‌شود.
    /// </summary>
    public static string Sanitize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message ?? string.Empty;
        message = Sensitive.Replace(message, "$1=***");
        message = StackCue.Replace(message, string.Empty);
        message = message.Replace('\n', ' ').Replace('\r', ' ');
        if (message.Contains('{') && message.Contains('}'))
        {
            message = "payload-omitted";
        }

        var combined = exception.GetType().Name + ": " + message;
        if (combined.Length <= MaxLength)
        {
            return combined;
        }

        return combined[..MaxLength];
    }
}
