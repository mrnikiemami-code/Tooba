namespace Tooba.BuildingBlocks.Observability.Correlation;

/// <summary>
/// SSOT همبستگی مبتنی بر <see cref="AsyncLocal{T}"/> — بدون mutable global خارج از AsyncLocal.
/// </summary>
public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string?> CurrentValue = new();

    /// <summary>شناسه فعال با فرمت N یا تهی.</summary>
    public static string? Current => CurrentValue.Value;

    /// <summary>همان <see cref="Current"/> به‌صورت Guid.</summary>
    public static Guid? CurrentGuid => TryParseGuid(CurrentValue.Value, out var guid) ? guid : null;

    /// <summary>مقداردهی اجباری پس از نرمال‌سازی به فرمت N.</summary>
    public static string Set(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        if (!TryNormalize(correlationId, out var normalized))
        {
            throw new ArgumentException(
                $"Invalid correlation id '{correlationId}'. Expected a GUID.",
                nameof(correlationId));
        }

        CurrentValue.Value = normalized;
        return normalized;
    }

    /// <summary>
    /// تضمین وجود شناسه: مقدار فعال، incoming نرمال‌شده، یا GUID جدید پایدار برای این جریان.
    /// </summary>
    public static string Ensure(string? incoming = null)
    {
        if (!string.IsNullOrWhiteSpace(CurrentValue.Value))
        {
            return CurrentValue.Value!;
        }

        if (!string.IsNullOrWhiteSpace(incoming) && TryNormalize(incoming, out var normalizedIncoming))
        {
            CurrentValue.Value = normalizedIncoming;
            return normalizedIncoming;
        }

        var generated = Guid.NewGuid().ToString("N");
        CurrentValue.Value = generated;
        return generated;
    }

    /// <summary>Scope تو در تو؛ با Dispose مقدار قبلی بازیابی می‌شود.</summary>
    public static IDisposable BeginScope(string correlationId)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = TryNormalize(correlationId, out var normalized)
            ? normalized
            : Ensure();
        return new Scope(previous);
    }

    /// <summary>تلاش برای parse به Guid.</summary>
    public static bool TryParseGuid(string? value, out Guid guid)
    {
        guid = default;
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out guid);
    }

    /// <summary>نرمال‌سازی به فرمت N (۳۲ hex).</summary>
    public static bool TryNormalize(string value, out string normalized)
    {
        normalized = string.Empty;
        if (!Guid.TryParse(value.Trim(), out var guid))
        {
            return false;
        }

        normalized = guid.ToString("N");
        return true;
    }

    /// <summary>پاک‌سازی AsyncLocal برای تست‌های ایزوله.</summary>
    internal static void Clear() => CurrentValue.Value = null;

    private sealed class Scope(string? previous) : IDisposable
    {
        public void Dispose() => CurrentValue.Value = previous;
    }
}
