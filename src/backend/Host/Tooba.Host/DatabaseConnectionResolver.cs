using Npgsql;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// پیاده‌سازی Host برای تبدیل <see cref="ConnectionReference"/> به رشتهٔ Npgsql از پیکربندی.
/// رشتهٔ اتصال پارس می‌شود اما هرگز لاگ یا به کلاینت برنمی‌گردد.
/// </summary>
internal sealed class DatabaseConnectionResolver : IDatabaseConnectionResolver
{
    private readonly ToobaPlatformOptions _options;

    /// <summary>
    /// resolver را به options فرآیند وصل می‌کند.
    /// </summary>
    public DatabaseConnectionResolver(Microsoft.Extensions.Options.IOptions<ToobaPlatformOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public string Resolve(ConnectionReference reference)
    {
        if (string.IsNullOrWhiteSpace(reference.Value)
            || !_options.PostgreSQL.ConnectionReferences.TryGetValue(reference.Value, out var connectionString)
            || string.IsNullOrWhiteSpace(connectionString))
        {
            throw new PlatformHttpException(
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                "platform.connection.unconfigured");
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new PlatformHttpException(
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                "platform.connection.unconfigured");
        }

        return connectionString;
    }
}
