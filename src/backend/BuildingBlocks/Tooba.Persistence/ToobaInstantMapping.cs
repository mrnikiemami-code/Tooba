using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace Tooba.Persistence;

/// <summary>
/// تبدیل Instant به DateTimeOffset برای Npgsql بدون افزونهٔ سراسری NodaTime.
/// افزونهٔ سراسری Dapper/MassTransit را برای ستون timestamptz می‌شکند.
/// </summary>
public static class ToobaInstantMapping
{
    private static readonly ValueConverter<Instant, DateTimeOffset> Required =
        new(value => value.ToDateTimeOffset(), value => Instant.FromDateTimeOffset(value));

    private static readonly ValueConverter<Instant?, DateTimeOffset?> Optional =
        new(
            value => value.HasValue ? value.Value.ToDateTimeOffset() : null,
            value => value.HasValue ? Instant.FromDateTimeOffset(value.Value) : null);

    /// <summary>
    /// ستون اجباری Instant را بدون UseNodaTime سراسری نگاشت می‌کند.
    /// </summary>
    public static PropertyBuilder<Instant> MapAsTimestamp(this PropertyBuilder<Instant> property) =>
        property.HasConversion(Required);

    /// <summary>
    /// ستون اختیاری Instant را بدون UseNodaTime سراسری نگاشت می‌کند.
    /// </summary>
    public static PropertyBuilder<Instant?> MapAsTimestamp(this PropertyBuilder<Instant?> property) =>
        property.HasConversion(Optional);
}
