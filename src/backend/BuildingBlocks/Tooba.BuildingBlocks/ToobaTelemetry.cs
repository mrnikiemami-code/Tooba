namespace Tooba.BuildingBlocks;

/// <summary>
/// نام‌های پایدار منبع Activity و Meter برای OpenTelemetry در کل فرآیند Tooba.
/// لاگ فنی و رویداد Audit کسب‌وکار از هم جدا می‌مانند؛ این نوع فقط تله‌متری فنی است.
/// </summary>
public static class ToobaTelemetry
{
    /// <summary>
    /// نام ActivitySource ثبت‌شده در Host برای tracing.
    /// </summary>
    public const string ActivitySourceName = "Tooba";

    /// <summary>
    /// نام Meter ثبت‌شده در Host برای metrics.
    /// </summary>
    public const string MeterName = "Tooba";

    /// <summary>
    /// منبع spanهای سفارشی Tooba؛ مسیرهای health/ready نباید نویز tracing تولید کنند.
    /// </summary>
    public static readonly System.Diagnostics.ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>
    /// Meter سفارشی Tooba برای شمارنده‌ها و quantiles فنی (نه KPI کسب‌وکار).
    /// </summary>
    public static readonly System.Diagnostics.Metrics.Meter Meter = new(MeterName);
}
