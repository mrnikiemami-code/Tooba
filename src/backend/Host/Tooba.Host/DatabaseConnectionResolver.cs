using Npgsql;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

internal interface IDatabaseConnectionResolver
{
    string Resolve(ConnectionReference reference);
}

internal sealed class DatabaseConnectionResolver : IDatabaseConnectionResolver
{
    private readonly ToobaPlatformOptions _options;

    public DatabaseConnectionResolver(Microsoft.Extensions.Options.IOptions<ToobaPlatformOptions> options)
    {
        _options = options.Value;
    }

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
