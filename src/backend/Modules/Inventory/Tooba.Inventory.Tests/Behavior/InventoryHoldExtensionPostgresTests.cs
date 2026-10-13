using System.Data;
using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Inventory.Tests.Behavior;

/// <summary>
/// Behavioural PostgreSQL verification for the atomic hold extension: all-or-none, monotonic replay,
/// NULL-TTL preservation, missing/empty ids, deadline validation against the database clock, a
/// genuinely blocking row lock held for the whole call and released afterwards, and connection
/// ownership. It runs against a DISPOSABLE database created for the test on the same local server and
/// never connects to, seeds or locks the live <c>tooba_alpha</c> database. If no local PostgreSQL with
/// the Development credentials is reachable the test returns without claiming anything.
///
/// The order: only the first test is a full scenario per behaviour, because each creates and drops its
/// own database, keeping the live Host on :5088 untouched.
/// </summary>
public sealed class InventoryHoldExtensionPostgresTests
{
    [Fact]
    public async Task Extends_every_finite_hold_to_the_requested_deadline()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var seedExpiry = now.AddMinutes(10);
        var earliest = Guid.Parse("aa000000-0000-7000-8000-000000000001");
        var later = Guid.Parse("aa000000-0000-7000-8000-000000000002");
        await database.InsertReservationAsync(earliest, "Held", seedExpiry);
        await database.InsertReservationAsync(later, "Held", seedExpiry.AddMinutes(5));
        var deadline = now.AddHours(2);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);
            var result = await directory.ExtendHeldForPaymentAsync([earliest, later], deadline, CancellationToken.None);

            Assert.Equal(InventoryPaymentHoldExtensionOutcome.Extended, result.Outcome);
            Assert.True(result.AllHeldAndExtended);
            Assert.Equal(2, result.RequestedCount);
            Assert.Empty(result.UnavailableReservationIds);
            AssertSameInstant(deadline, result.GuaranteedUntil);

            // Connection ownership stays with the DbContext: EF closes what EF opened.
            Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
        }

        AssertSameInstant(deadline, await database.ReadExpiryAsync(earliest));
        AssertSameInstant(deadline, await database.ReadExpiryAsync(later));
    }

    [Fact]
    public async Task One_released_hold_rejects_the_whole_set_and_changes_nothing()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var seedExpiry = now.AddMinutes(10);
        var held1 = Guid.Parse("bb000000-0000-7000-8000-000000000001");
        var released = Guid.Parse("bb000000-0000-7000-8000-000000000002");
        var held2 = Guid.Parse("bb000000-0000-7000-8000-000000000003");
        await database.InsertReservationAsync(held1, "Held", seedExpiry);
        await database.InsertReservationAsync(released, "Released", seedExpiry);
        await database.InsertReservationAsync(held2, "Held", seedExpiry);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);
            var result = await directory.ExtendHeldForPaymentAsync(
                [held1, released, held2],
                now.AddHours(2),
                CancellationToken.None);

            Assert.Equal(InventoryPaymentHoldExtensionOutcome.NotHeld, result.Outcome);
            Assert.False(result.AllHeldAndExtended);
            Assert.Equal([released], result.UnavailableReservationIds);
        }

        // All-or-none: the two provable holds were NOT extended either.
        AssertSameInstant(seedExpiry, await database.ReadExpiryAsync(held1));
        AssertSameInstant(seedExpiry, await database.ReadExpiryAsync(held2));
    }

    [Fact]
    public async Task Expired_and_consumed_and_missing_holds_fail_closed()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var seedExpiry = now.AddMinutes(10);
        var expired = Guid.Parse("cc000000-0000-7000-8000-000000000001");
        var consumed = Guid.Parse("cc000000-0000-7000-8000-000000000002");
        var held = Guid.Parse("cc000000-0000-7000-8000-000000000003");
        var missing = Guid.Parse("cc000000-0000-7000-8000-0000000000ff");
        await database.InsertReservationAsync(expired, "Held", now.AddMinutes(-5));
        await database.InsertReservationAsync(consumed, "Consumed", seedExpiry);
        await database.InsertReservationAsync(held, "Held", seedExpiry);
        var deadline = now.AddHours(2);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);

            var expiredResult = await directory.ExtendHeldForPaymentAsync([expired, held], deadline, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.NotHeld, expiredResult.Outcome);
            Assert.Equal([expired], expiredResult.UnavailableReservationIds);

            var consumedResult = await directory.ExtendHeldForPaymentAsync([consumed, held], deadline, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.NotHeld, consumedResult.Outcome);
            Assert.Equal([consumed], consumedResult.UnavailableReservationIds);

            var missingResult = await directory.ExtendHeldForPaymentAsync([missing, held], deadline, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.NotHeld, missingResult.Outcome);
            Assert.Equal([missing], missingResult.UnavailableReservationIds);

            var emptyGuidResult = await directory.ExtendHeldForPaymentAsync([Guid.Empty, held], deadline, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.NotHeld, emptyGuidResult.Outcome);
            Assert.Equal([Guid.Empty], emptyGuidResult.UnavailableReservationIds);
        }

        // Nothing was ever revived or released, and the provable hold was never partially extended.
        AssertSameInstant(now.AddMinutes(-5), await database.ReadExpiryAsync(expired));
        AssertSameInstant(seedExpiry, await database.ReadExpiryAsync(consumed));
        AssertSameInstant(seedExpiry, await database.ReadExpiryAsync(held));
        Assert.Equal("Held", await database.ReadStatusAsync(held));
    }

    [Fact]
    public async Task Replay_is_monotonic_and_never_shortens_a_hold()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var reservationId = Guid.Parse("dd000000-0000-7000-8000-000000000001");
        var seedExpiry = now.AddMinutes(10);
        await database.InsertReservationAsync(reservationId, "Held", seedExpiry);
        var longDeadline = now.AddHours(6);
        var shortDeadline = now.AddMinutes(20);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);

            var first = await directory.ExtendHeldForPaymentAsync([reservationId], longDeadline, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.Extended, first.Outcome);

            // A later, shorter request must not pull the deadline back.
            var shorter = await directory.ExtendHeldForPaymentAsync([reservationId], shortDeadline, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.Extended, shorter.Outcome);
            AssertSameInstant(longDeadline, shorter.GuaranteedUntil);
            AssertSameInstant(longDeadline, await database.ReadExpiryAsync(reservationId));

            // A longer request may move it forward again.
            var longer = now.AddHours(9);
            var extendedAgain = await directory.ExtendHeldForPaymentAsync([reservationId], longer, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.Extended, extendedAgain.Outcome);
            AssertSameInstant(longer, extendedAgain.GuaranteedUntil);
        }

        AssertSameInstant(now.AddHours(9), await database.ReadExpiryAsync(reservationId));
    }

    [Fact]
    public async Task Durable_null_expiry_is_preserved_and_never_given_a_deadline()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var durable = Guid.Parse("ee000000-0000-7000-8000-000000000001");
        var finite = Guid.Parse("ee000000-0000-7000-8000-000000000002");
        var now2 = now.AddMinutes(10);
        await database.InsertReservationAsync(durable, "Held", null);
        await database.InsertReservationAsync(finite, "Held", now2);
        var deadline = now.AddHours(2);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);
            var result = await directory.ExtendHeldForPaymentAsync([durable, finite], deadline, CancellationToken.None);

            Assert.Equal(InventoryPaymentHoldExtensionOutcome.Extended, result.Outcome);
            // The durable hold carries no deadline, so the weakest guarantee is the finite one.
            AssertSameInstant(deadline, result.GuaranteedUntil);
        }

        Assert.Null(await database.ReadExpiryAsync(durable));
        AssertSameInstant(deadline, await database.ReadExpiryAsync(finite));
    }

    [Fact]
    public async Task Only_durable_holds_report_no_guaranteed_deadline()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var durable = Guid.Parse("ef000000-0000-7000-8000-000000000001");
        await database.InsertReservationAsync(durable, "Held", null);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);
            var result = await directory.ExtendHeldForPaymentAsync([durable], now.AddHours(2), CancellationToken.None);

            Assert.Equal(InventoryPaymentHoldExtensionOutcome.Extended, result.Outcome);
            Assert.Null(result.GuaranteedUntil);
        }

        Assert.Null(await database.ReadExpiryAsync(durable));
    }

    [Fact]
    public async Task Deadline_already_past_the_database_clock_fails_closed()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var seedExpiry = now.AddMinutes(10);
        var reservationId = Guid.Parse("ff000000-0000-7000-8000-000000000001");
        await database.InsertReservationAsync(reservationId, "Held", seedExpiry);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);

            var past = await directory.ExtendHeldForPaymentAsync([reservationId], now.AddHours(-1), CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.DeadlineInvalid, past.Outcome);
            Assert.Null(past.GuaranteedUntil);

            var absent = await directory.ExtendHeldForPaymentAsync([reservationId], default, CancellationToken.None);
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.DeadlineInvalid, absent.Outcome);
        }

        AssertSameInstant(seedExpiry, await database.ReadExpiryAsync(reservationId));
    }

    [Fact]
    public async Task Ambient_scope_is_refused_and_leaves_the_hold_untouched()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var seedExpiry = now.AddMinutes(10);
        var reservationId = Guid.Parse("a1000000-0000-7000-8000-000000000001");
        await database.InsertReservationAsync(reservationId, "Held", seedExpiry);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);
            using var ambient = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
            var result = await directory.ExtendHeldForPaymentAsync([reservationId], now.AddHours(2), CancellationToken.None);

            Assert.Equal(InventoryPaymentHoldExtensionOutcome.AmbientTransactionUnsupported, result.Outcome);
        }

        AssertSameInstant(seedExpiry, await database.ReadExpiryAsync(reservationId));
    }

    [Fact]
    public async Task Row_lock_blocks_a_competitor_for_the_whole_call_and_is_released_afterwards()
    {
        await using var database = await DisposableInventoryDatabase.TryCreateAsync();
        if (database is null) return;

        var now = await database.ReadDatabaseNowAsync();
        var seedExpiry = now.AddMinutes(10);
        var reservationId = Guid.Parse("a2000000-0000-7000-8000-000000000001");
        await database.InsertReservationAsync(reservationId, "Held", seedExpiry);
        var deadline = now.AddHours(2);

        await using (var db = database.CreateContext())
        {
            var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);

            // A competitor holds the row first: the extension must block, never proceed optimistically.
            await using var competitor = new NpgsqlConnection(database.ConnectionString);
            await competitor.OpenAsync();
            await using var competitorTransaction = await competitor.BeginTransactionAsync();
            await using (var blocker = competitor.CreateCommand())
            {
                blocker.Transaction = competitorTransaction;
                blocker.CommandText = "SELECT reservation_id FROM inventory.reservations WHERE reservation_id = @id FOR UPDATE";
                blocker.Parameters.AddWithValue("id", reservationId);
                await using var reader = await blocker.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
            }

            var call = directory.ExtendHeldForPaymentAsync([reservationId], deadline, CancellationToken.None);
            await Task.Delay(700);
            Assert.False(call.IsCompleted, "extension must block on the row lock, not proceed without it");

            await competitorTransaction.RollbackAsync();

            var result = await call;
            Assert.Equal(InventoryPaymentHoldExtensionOutcome.Extended, result.Outcome);
            Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
        }

        AssertSameInstant(deadline, await database.ReadExpiryAsync(reservationId));

        // The lock was released with the call's transaction: a competing FOR UPDATE now succeeds at once.
        await using var after = new NpgsqlConnection(database.ConnectionString);
        await after.OpenAsync();
        await using (var lockTimeout = after.CreateCommand())
        {
            lockTimeout.CommandText = "SET lock_timeout = '750ms'";
            await lockTimeout.ExecuteNonQueryAsync();
        }

        await using var contender = after.CreateCommand();
        contender.CommandText = "SELECT reservation_id FROM inventory.reservations WHERE reservation_id = @id FOR UPDATE";
        contender.Parameters.AddWithValue("id", reservationId);
        await using var contenderReader = await contender.ExecuteReaderAsync();
        Assert.True(await contenderReader.ReadAsync());
    }

    private static void AssertSameInstant(DateTimeOffset? expected, DateTimeOffset? actual)
    {
        if (expected is null || actual is null)
        {
            Assert.Equal(expected, actual);
            return;
        }

        Assert.True(
            Math.Abs((expected.Value - actual.Value).TotalSeconds) < 0.001,
            $"expected {expected.Value:O} but was {actual.Value:O}");
    }

    /// <summary>
    /// A disposable <c>inventory.reservations</c> database created for one test on the same local
    /// PostgreSQL server. Credentials come from the repository's Development settings; the live
    /// <c>tooba_alpha</c> database is only read for that purpose and never connected to.
    /// </summary>
    private sealed class DisposableInventoryDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;

        private DisposableInventoryDatabase(string name, string adminConnectionString)
        {
            Name = name;
            _adminConnectionString = adminConnectionString;
            ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                Database = name,
            }.ConnectionString;
        }

        public string Name { get; }

        public string ConnectionString { get; }

        public static async Task<DisposableInventoryDatabase?> TryCreateAsync()
        {
            if (!TryResolveAdminConnectionString(out var admin))
            {
                return null;
            }

            try
            {
                await using (var probe = new NpgsqlConnection(admin))
                {
                    await probe.OpenAsync();
                    await using var ping = probe.CreateCommand();
                    ping.CommandText = "SELECT 1";
                    await ping.ExecuteScalarAsync();
                }
            }
            catch (Exception)
            {
                // No isolated PostgreSQL available here: nothing is proven and nothing is claimed.
                return null;
            }

            var name = $"tooba_hold017_{Guid.NewGuid():N}"[..28];
            var database = new DisposableInventoryDatabase(name, admin);
            await database.ExecuteAdminAsync($"CREATE DATABASE \"{name}\"");
            await database.ExecuteAsync(
                """
                CREATE SCHEMA IF NOT EXISTS inventory;
                CREATE TABLE inventory.reservations (
                    reservation_id uuid NOT NULL PRIMARY KEY,
                    stock_item_id uuid NOT NULL,
                    quantity numeric(18,6) NOT NULL,
                    status character varying(32) NOT NULL,
                    external_reference character varying(128),
                    idempotency_key character varying(128),
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    expires_at timestamp with time zone
                );
                """);
            return database;
        }

        public InventoryDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>();
            ToobaNpgsql.ConfigureModuleContext(
                options,
                ConnectionString,
                InventoryDbContext.Schema,
                typeof(InventoryDbContext));
            return new InventoryDbContext(options.Options);
        }

        public Task InsertReservationAsync(Guid reservationId, string status, DateTimeOffset? expiresAt) =>
            ExecuteAsync(
                """
                INSERT INTO inventory.reservations
                    (reservation_id, stock_item_id, quantity, status, created_at, updated_at, expires_at)
                VALUES
                    (@id, @stockItemId, 2, @status, clock_timestamp(), clock_timestamp(), @expiresAt)
                """,
                command =>
                {
                    command.Parameters.AddWithValue("id", reservationId);
                    command.Parameters.AddWithValue("stockItemId", Guid.Parse("0f000000-0000-7000-8000-00000000000f"));
                    command.Parameters.AddWithValue("status", status);
                    var expiry = new NpgsqlParameter("expiresAt", NpgsqlDbType.TimestampTz);
                    expiry.Value = expiresAt is { } finite ? finite : DBNull.Value;
                    command.Parameters.Add(expiry);
                });

        public Task<DateTimeOffset?> ReadExpiryAsync(Guid reservationId) =>
            ReadAsync<DateTimeOffset?>(
                "SELECT expires_at FROM inventory.reservations WHERE reservation_id = @id",
                reservationId,
                reader => reader.IsDBNull(0) ? null : AsUtc(reader.GetValue(0)));

        public Task<string?> ReadStatusAsync(Guid reservationId) =>
            ReadAsync<string?>(
                "SELECT status FROM inventory.reservations WHERE reservation_id = @id",
                reservationId,
                reader => reader.GetString(0));

        public Task<DateTimeOffset> ReadDatabaseNowAsync() =>
            ReadAsync(
                "SELECT clock_timestamp()",
                reservationId: null,
                reader => AsUtc(reader.GetValue(0)));

        public async ValueTask DisposeAsync()
        {
            try
            {
                await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)");
            }
            catch (Exception)
            {
                // Cleanup is best effort: a leftover throwaway database is not evidence of a defect.
            }
        }

        private static bool TryResolveAdminConnectionString(out string adminConnectionString)
        {
            adminConnectionString = string.Empty;
            var root = FindRepositoryRoot();
            if (root is null)
            {
                return false;
            }

            var settings = Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "appsettings.Development.json");
            if (!File.Exists(settings))
            {
                return false;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(settings));
            if (!document.RootElement.TryGetProperty("Tooba", out var tooba)
                || !tooba.TryGetProperty("PostgreSQL", out var postgres)
                || !postgres.TryGetProperty("ConnectionReferences", out var references)
                || !references.TryGetProperty("tenant-alpha", out var tenant))
            {
                return false;
            }

            adminConnectionString = new NpgsqlConnectionStringBuilder(tenant.GetString() ?? string.Empty)
            {
                Database = "postgres",
                Timeout = 3,
                CommandTimeout = 10,
            }.ConnectionString;
            return true;
        }

        private static string? FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static DateTimeOffset AsUtc(object value) => value switch
        {
            DateTimeOffset offset => offset.ToUniversalTime(),
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => throw new InvalidCastException("unexpected timestamp representation"),
        };

        private async Task<T> ReadAsync<T>(
            string sql,
            Guid? reservationId,
            Func<NpgsqlDataReader, T> read)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            if (reservationId is { } id)
            {
                command.Parameters.AddWithValue("id", id);
            }

            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            return read(reader);
        }

        private Task ExecuteAsync(string sql, Action<NpgsqlCommand>? configure = null) =>
            ExecuteCoreAsync(ConnectionString, sql, configure);

        private Task ExecuteAdminAsync(string sql) => ExecuteCoreAsync(_adminConnectionString, sql, null);

        private static async Task ExecuteCoreAsync(string connectionString, string sql, Action<NpgsqlCommand>? configure)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            configure?.Invoke(command);
            await command.ExecuteNonQueryAsync();
        }
    }
}
