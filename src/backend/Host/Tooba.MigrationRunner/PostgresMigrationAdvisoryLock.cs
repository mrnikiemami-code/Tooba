using Npgsql;

namespace Tooba.MigrationRunner;

/// <summary>
/// قفل advisory PostgreSQL برای جلوگیری از apply همزمان روی یک پایگاه.
/// </summary>
internal sealed class PostgresMigrationAdvisoryLock : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly long _lockKey;
    private bool _held;

    private PostgresMigrationAdvisoryLock(NpgsqlConnection connection, long lockKey, bool held)
    {
        _connection = connection;
        _lockKey = lockKey;
        _held = held;
    }

    /// <summary>
    /// تلاش برای acquire قفل با timeout مشخص.
    /// </summary>
    internal static async Task<PostgresMigrationAdvisoryLock?> TryAcquireAsync(
        string connectionString,
        string scopeKey,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var lockKey = ComputeLockKey(scopeKey);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", lockKey);
            var acquired = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
            if (acquired)
            {
                return new PostgresMigrationAdvisoryLock(connection, lockKey, held: true);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        await connection.DisposeAsync();
        return null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_held)
        {
            await _connection.DisposeAsync();
            return;
        }

        await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", _connection);
        command.Parameters.AddWithValue("key", _lockKey);
        _ = await command.ExecuteScalarAsync();
        _held = false;
        await _connection.DisposeAsync();
    }

    private static long ComputeLockKey(string scopeKey)
    {
        var hash = scopeKey.GetHashCode(StringComparison.Ordinal);
        return ((long)hash << 32) | (uint)"TOOBA".GetHashCode(StringComparison.Ordinal);
    }
}
