using NodaTime;
using Npgsql;

namespace Tooba.Persistence;

/// <summary>
/// Claim و به‌روزرسانی Outbox با SQL خام PostgreSQL. هر فراخوانی اتصال جدا می‌گیرد تا Tenantها DbContext مشترک نداشته باشند.
/// </summary>
public sealed class NpgsqlOutboxDispatcherStore : IOutboxDispatcherStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> ClaimAsync(
        string connectionString,
        string schema,
        string tableName,
        int batchSize,
        int lockSeconds,
        CancellationToken cancellationToken)
    {
        var table = Qualified(schema, tableName);
        var sql = $"""
            WITH claimed AS (
              SELECT id
              FROM {table}
              WHERE processed_at IS NULL
                AND dead_lettered_at IS NULL
                AND (next_attempt_at IS NULL OR next_attempt_at <= now())
                AND (locked_until IS NULL OR locked_until <= now())
              ORDER BY occurred_at
              LIMIT @batch
              FOR UPDATE SKIP LOCKED
            )
            UPDATE {table} AS o
            SET locked_until = now() + (@lock_seconds * interval '1 second'),
                attempt_count = o.attempt_count + 1
            FROM claimed
            WHERE o.id = claimed.id
            RETURNING o.id, o.occurred_at, o.event_type, o.payload, o.correlation_id, o.version,
                      o.tenant_id, o.deployment_id, o.edition, o.processed_at, o.attempt_count,
                      o.next_attempt_at, o.dead_lettered_at, o.last_error, o.locked_until
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("batch", batchSize);
        command.Parameters.AddWithValue("lock_seconds", lockSeconds);
        var rows = new List<OutboxMessage>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(Read(reader));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return rows;
    }

    /// <inheritdoc />
    public Task MarkProcessedAsync(
        string connectionString,
        string schema,
        string tableName,
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            connectionString,
            $"""
             UPDATE {Qualified(schema, tableName)}
             SET processed_at = now(), locked_until = NULL, last_error = NULL
             WHERE id = @id
             """,
            command => command.Parameters.AddWithValue("id", id),
            cancellationToken);

    /// <inheritdoc />
    public Task MarkRetryAsync(
        string connectionString,
        string schema,
        string tableName,
        Guid id,
        Instant nextAttemptAt,
        string lastError,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            connectionString,
            $"""
             UPDATE {Qualified(schema, tableName)}
             SET next_attempt_at = @next_attempt,
                 last_error = @last_error,
                 locked_until = NULL
             WHERE id = @id
             """,
            command =>
            {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("next_attempt", nextAttemptAt.ToDateTimeUtc());
                command.Parameters.AddWithValue("last_error", lastError);
            },
            cancellationToken);

    /// <inheritdoc />
    public Task MarkDeadLetterAsync(
        string connectionString,
        string schema,
        string tableName,
        Guid id,
        string lastError,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            connectionString,
            $"""
             UPDATE {Qualified(schema, tableName)}
             SET dead_lettered_at = now(),
                 last_error = @last_error,
                 locked_until = NULL
             WHERE id = @id
             """,
            command =>
            {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("last_error", lastError);
            },
            cancellationToken);

    private static string Qualified(string schema, string table) =>
        SqlIdentifiers.Quote(schema) + "." + SqlIdentifiers.Quote(table);

    private static async Task ExecuteAsync(
        string connectionString,
        string sql,
        Action<NpgsqlCommand> bind,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static OutboxMessage Read(Npgsql.NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        OccurredAt = Instant.FromDateTimeUtc(DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc)),
        EventType = reader.GetString(2),
        Payload = reader.GetString(3),
        CorrelationId = reader.IsDBNull(4) ? null : reader.GetString(4),
        Version = reader.GetInt32(5),
        TenantId = reader.IsDBNull(6) ? null : reader.GetString(6),
        DeploymentId = reader.GetString(7),
        Edition = reader.GetString(8),
        ProcessedAt = reader.IsDBNull(9) ? null : Instant.FromDateTimeUtc(DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc)),
        AttemptCount = reader.GetInt32(10),
        NextAttemptAt = reader.IsDBNull(11) ? null : Instant.FromDateTimeUtc(DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc)),
        DeadLetteredAt = reader.IsDBNull(12) ? null : Instant.FromDateTimeUtc(DateTime.SpecifyKind(reader.GetDateTime(12), DateTimeKind.Utc)),
        LastError = reader.IsDBNull(13) ? null : reader.GetString(13),
        LockedUntil = reader.IsDBNull(14) ? null : Instant.FromDateTimeUtc(DateTime.SpecifyKind(reader.GetDateTime(14), DateTimeKind.Utc)),
    };
}
