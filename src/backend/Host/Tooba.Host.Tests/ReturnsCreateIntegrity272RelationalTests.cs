using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Persistence;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-272 / TB-272-R1 (A, B, D) on REAL PostgreSQL, under a ROLLBACK-ONLY boundary.
/// <para>
/// Every task-owned row is written inside a CALLER-OWNED transaction that the test itself rolls back
/// (<c>await using</c> dispose ⇒ rollback); nothing is ever committed and nothing is deleted afterwards. The
/// creation therefore runs exactly the "caller already owns a transaction" branch of the owning directory, and each
/// fact ends by re-reading the canonical database on a separate connection to prove the rolled-back rows are gone.
/// </para>
/// <para>
/// The target is validated BEFORE any write: <c>TB272_CONNECTION</c> must address loopback PostgreSQL port 5432 and
/// database <c>tooba_alpha</c>, and the server itself must confirm that identity read-only. A missing or non-canonical
/// target produces a visible framework SKIP (never a silent pass) and no connection is opened against it.
/// </para>
/// <para>
/// The Order/Fulfillment/payment/wallet seams stay controlled in-memory readers: no provider is called, no money
/// moves, no stock changes. What is NOT demonstrated here is a committed winner-vs-loser two-writer race: a competing
/// row can only be seen by another transaction once it is committed, which this boundary forbids — that limit is
/// declared in the evidence instead of being manufactured.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class ReturnsCreateIntegrity272RelationalTests
{
    private static readonly DateTimeOffset DeliveredAt = DateTimeOffset.UtcNow.AddDays(-1);

    /// <summary>
    /// The canonical target check, evaluated ONCE per run. It never logs or returns the connection string itself, only
    /// whether the target is the canonical loopback <c>tooba_alpha</c> database.
    /// </summary>
    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);

    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb272r1-rel-{Guid.NewGuid():N}-";

    public ReturnsCreateIntegrity272RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB272_CONNECTION") is { Length: > 0 } raw ? raw.Trim() : null;

    // ------------------------------------------------------------------ D: the advisory lock is the serialization

    [SkippableFact]
    public async Task The_creation_decision_waits_for_the_seller_order_advisory_lock()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var customer = Guid.NewGuid();
        var orders = new MapOrderReader([BuildOrder(sellerOrderId, customer, 2m, [lineId])]);
        var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();
        var directory = BuildDirectory(db, orders, fulfillments);

        // Another session holds THIS order's creation lock. If the writer did not take the same lock it would finish
        // while the foreign transaction is still open.
        await using var holder = new NpgsqlConnection(Connection);
        await holder.OpenAsync();
        await using var held = await holder.BeginTransactionAsync();
        await using (var lockCommand = new NpgsqlCommand(
            $"SELECT pg_advisory_xact_lock({ReturnCreationSerialization.AdvisoryLockKey(sellerOrderId)})",
            holder,
            held))
        {
            await lockCommand.ExecuteNonQueryAsync();
        }

        var pending = directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}lock", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        Assert.False(pending.IsCompleted);

        // Releasing the foreign transaction releases the writer, which then completes its own decision.
        await held.RollbackAsync();
        var snapshot = await pending.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(1m, snapshot.Items.Single().Quantity);
        _output.WriteLine($"TB272R1-D lock order={sellerOrderId} request={snapshot.ReturnRequestId}");

        // The caller's transaction is rolled back by disposal; nothing it wrote survives.
        await caller.RollbackAsync();
        AssertNoRolledBackRows(await CountTagRowsAsync());
    }

    // ------------------------------------------------------------------ B: real collision shape + savepoint recovery

    [SkippableFact]
    public async Task A_real_unique_violation_inside_a_caller_transaction_is_exactly_classified_and_savepoint_recoverable()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var key = $"{_runTag}real-index";
        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();

        var createdAt = DeliveredAt;
        var first = ReturnRequest.Create(
            Guid.NewGuid(), () => Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            key, "دلیل مرجوعی", "IRR", [(Guid.NewGuid(), 1m, 1000m, (Guid?)null)], createdAt);
        db.ReturnRequests.Add(first);
        db.ReturnItems.AddRange(first.Items);
        await db.SaveChangesAsync();

        // A savepoint exactly like the one the owning directory opens immediately BEFORE its own insert.
        await caller.CreateSavepointAsync(ReturnCreationSerialization.CallerSavepointName);

        // The SAME idempotency key inside the same transaction: PostgreSQL itself raises 23505 on the real index and
        // EF surfaces it exactly as the owning recovery branch sees it.
        var duplicate = ReturnRequest.Create(
            Guid.NewGuid(), () => Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            key, "دلیل مرجوعی", "IRR", [(Guid.NewGuid(), 1m, 1000m, (Guid?)null)], createdAt);
        db.ReturnRequests.Add(duplicate);
        db.ReturnItems.AddRange(duplicate.Items);
        Exception? failure = null;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        var updateException = Assert.IsType<DbUpdateException>(failure);
        var postgres = Assert.IsType<PostgresException>(updateException.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal(ReturnCreationSerialization.IdempotencyUniqueIndex, postgres.ConstraintName);
        Assert.Equal(ReturnCreationSerialization.IdempotencyTable, postgres.TableName);
        Assert.True(ReturnCreationSerialization.IsIdempotencyConflict(updateException));

        // Restoring the savepoint makes the CALLER's transaction usable again — the directory's recovery mechanism.
        // This test simply drops the failed attempt's tracked additions (the production recovery detaches only its own).
        await caller.RollbackToSavepointAsync(ReturnCreationSerialization.CallerSavepointName);
        db.ChangeTracker.Clear();

        var usable = await InsertRequestAsync(db, NewRow($"{_runTag}real-index-c", $"{_runTag}real-index-c"));
        Assert.NotNull(usable);
        Assert.Equal(1L, await db.ReturnRequests.AsNoTracking().LongCountAsync(x => x.IdempotencyKey == key));

        await caller.RollbackAsync();
        AssertNoRolledBackRows(await CountTagRowsAsync());
    }

    [SkippableFact]
    public async Task A_key_collision_removes_only_this_attempt_and_keeps_the_caller_transaction_usable()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var otherOrder = Guid.NewGuid();
        var otherLine = Guid.NewGuid();
        var key = $"{_runTag}injected-collision";
        var orders = new MapOrderReader([
            BuildOrder(sellerOrderId, customer, 2m, [lineId]),
            BuildOrder(otherOrder, customer, 2m, [otherLine]),
        ]);
        var fulfillments = new MapFulfillmentReader([
            BuildFulfillment(sellerOrderId, (lineId, 2m)),
            BuildFulfillment(otherOrder, (otherLine, 2m)),
        ]);

        var injector = new ExactConflictInjector();
        await using var db = CreateDb(injector);
        await using var caller = await db.Database.BeginTransactionAsync();

        // Caller-owned work that is already tracked and DIRTY before the creation attempt.
        var pending = ReturnRequest.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            otherOrder,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            $"{_runTag}caller-work",
            "کار در جریان",
            "IRR",
            [(otherLine, 1m, 1000m, (Guid?)null)],
            DateTimeOffset.UtcNow);
        db.ReturnRequests.Add(pending);
        db.ReturnItems.AddRange(pending.Items);
        await db.SaveChangesAsync();
        var paymentId = Guid.NewGuid();
        pending.Approve(paymentId, DateTimeOffset.UtcNow);
        Assert.Equal(EntityState.Modified, db.Entry(pending).State);

        var directory = BuildDirectory(db, orders, fulfillments);
        injector.Arm();

        // The injected failure carries the EXACT provider error shape of the real index (proven separately above), so
        // the owning recovery branch really runs: savepoint restore → scoped detach → winner read.
        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, key, [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));

        // No winner is visible inside this uncommitted transaction, so the honest answer is the typed stale refusal;
        // crucially it is NOT a borrowed snapshot and NOT a silent success.
        Assert.Equal(ReturnsErrorCodes.Stale, conflict.Code);

        // Only this attempt's additions were removed; the caller's dirty entity kept its instance, state and values.
        var entry = db.Entry(pending);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Same(pending, entry.Entity);
        Assert.Equal(ReturnRequestStatus.Approved, entry.Entity.Status);
        Assert.Equal(paymentId, entry.Entity.PaymentId);
        Assert.DoesNotContain(
            db.ChangeTracker.Entries<ReturnRequest>(),
            x => x.State == EntityState.Added && x.Entity.IdempotencyKey == key);
        Assert.DoesNotContain(db.ChangeTracker.Entries<ReturnItem>(), x => x.State == EntityState.Added);

        // The caller's transaction was neither committed nor rolled back by the directory: it still works.
        var afterRecovery = await directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}after-recovery", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(1m, afterRecovery.Items.Single().Quantity);

        // The caller's own pending change is still pending and now saveable in the same transaction.
        await db.SaveChangesAsync();
        Assert.Equal(ReturnRequestStatus.Approved,
            (await db.ReturnRequests.AsNoTracking().SingleAsync(x => x.ReturnRequestId == pending.ReturnRequestId)).Status);

        await caller.RollbackAsync();
        AssertNoRolledBackRows(await CountTagRowsAsync());
    }

    // ------------------------------------------------------------------ A: only the real key index is a collision

    [SkippableFact]
    public async Task Only_the_idempotency_index_is_classified_as_a_key_collision()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var orders = new MapOrderReader([BuildOrder(sellerOrderId, customer, 2m, [lineId])]);
        var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();
        var created = await BuildDirectory(db, orders, fulfillments).CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}constraint", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        var item = await db.ReturnItems.AsNoTracking()
            .SingleAsync(x => x.ReturnRequestId == created.ReturnRequestId);

        // A DIFFERENT unique constraint of the same schema (the item primary key) must never be reclassified as the
        // idempotency collision: the typed conflict is reachable only through the exact index identity.
        Exception? failure = null;
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO returns.return_items
                    (return_item_id, return_request_id, order_line_id, quantity, unit_price_snapshot, currency,
                     condition, disposition)
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})
                """,
                item.ReturnItemId,
                item.ReturnRequestId,
                item.OrderLineId,
                item.Quantity,
                item.UnitPriceSnapshot,
                item.Currency,
                "Usable",
                "None");
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        var postgres = failure as PostgresException ?? failure?.InnerException as PostgresException;
        Assert.NotNull(postgres);
        Assert.Equal("pk_return_items", postgres!.ConstraintName);
        Assert.False(ReturnCreationSerialization.IsIdempotencyConflict(new DbUpdateException("test", postgres)));

        await caller.RollbackAsync();
        AssertNoRolledBackRows(await CountTagRowsAsync());
    }

    // ------------------------------------------------------------------ A (R1): persisted precision and binding

    [SkippableFact]
    public async Task Persisted_quantity_precision_and_the_full_binding_hold_inside_one_transaction()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var key = $"{_runTag}precision";
        var orders = new MapOrderReader([
            BuildOrder(sellerOrderId, customer, 2m, [lineId], checkoutId, sellerPartyId),
        ]);
        var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();

        // A stored case with the exact binding and a SIX-decimal quantity really lives in the transaction.
        var storedId = Guid.NewGuid();
        await InsertRequestAsync(db, new RequestRow(
            storedId, sellerOrderId, checkoutId, sellerPartyId, customer, key, "دلیل مرجوعی", "Requested",
            0.001m, "OriginalPayment", "CustomerDelivered"));
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO returns.return_items
                (return_item_id, return_request_id, order_line_id, quantity, unit_price_snapshot, currency,
                 condition, disposition)
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})
            """,
            Guid.NewGuid(),
            storedId,
            lineId,
            0.000001m,
            1000m,
            "IRR",
            "Usable",
            "None");

        // Exact replay answers the PERSISTED facts: the stored six-decimal quantity is the answer, not a rounded one.
        var replay = await BuildDirectory(db, orders, fulfillments).CreateAsync(
            Command(sellerOrderId, customer, key, [new ReturnLineCommand(lineId, 0.000001m)]),
            CancellationToken.None);
        Assert.Equal(storedId, replay.ReturnRequestId);
        Assert.Equal(0.000001m, replay.Items.Single().Quantity);
        var persistedQuantity = await db.ReturnItems.AsNoTracking()
            .Where(x => x.ReturnRequestId == storedId)
            .Select(x => x.Quantity)
            .SingleAsync();
        Assert.Equal(0.000001m, persistedQuantity);
        Assert.Equal(replay.Items.Single().Quantity, persistedQuantity);

        // A quantity the column cannot represent exactly is refused BEFORE the stored key is even compared, so it can
        // never be answered by the snapshot above.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => BuildDirectory(db, orders, fulfillments)
            .CreateAsync(
                Command(sellerOrderId, customer, key, [new ReturnLineCommand(lineId, 0.0000011m)]),
                CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityPrecision, refused.Code);

        // The stored binding includes checkout and seller party: a same-key row carrying another checkout is refused
        // with no payload and no write.
        var otherCheckoutKey = $"{_runTag}binding";
        await InsertRequestAsync(db, new RequestRow(
            Guid.NewGuid(), sellerOrderId, Guid.NewGuid(), sellerPartyId, customer, otherCheckoutKey,
            "دلیل مرجوعی", "Requested", 1000m, "OriginalPayment", "CustomerDelivered"));
        var binding = await Assert.ThrowsAsync<ContractOperationException>(() => BuildDirectory(db, orders, fulfillments)
            .CreateAsync(
                Command(sellerOrderId, customer, otherCheckoutKey, [new ReturnLineCommand(lineId, 1m)]),
                CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, binding.Code);

        // A same-key row carrying another SELLER PARTY is refused the same way.
        var otherSellerKey = $"{_runTag}binding-seller";
        await InsertRequestAsync(db, new RequestRow(
            Guid.NewGuid(), sellerOrderId, checkoutId, Guid.NewGuid(), customer, otherSellerKey,
            "دلیل مرجوعی", "Requested", 1000m, "OriginalPayment", "CustomerDelivered"));
        var sellerBinding = await Assert.ThrowsAsync<ContractOperationException>(() => BuildDirectory(db, orders, fulfillments)
            .CreateAsync(
                Command(sellerOrderId, customer, otherSellerKey, [new ReturnLineCommand(lineId, 1m)]),
                CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, sellerBinding.Code);

        // Only the three injected rows exist in this transaction; the refusals wrote nothing.
        Assert.Equal(3, await db.ReturnRequests.AsNoTracking().CountAsync(x => x.IdempotencyKey.StartsWith(_runTag)));

        await caller.RollbackAsync();
        AssertNoRolledBackRows(await CountTagRowsAsync());
    }

    // ------------------------------------------------------------------ refusal + replay + rollback-only

    [SkippableFact]
    public async Task A_refused_creation_leaves_no_row_keeps_the_caller_transaction_and_replays_exactly()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var orders = new MapOrderReader([BuildOrder(sellerOrderId, customer, 1m, [lineId])]);
        var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 1m))]);

        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();
        var directory = BuildDirectory(db, orders, fulfillments);
        var first = await directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}rollback-1", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);

        // Over-asking the exhausted line is refused inside the critical section; a C# refusal is not a provider
        // failure, so the caller's transaction stays usable and no row was added by the refused attempt.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}rollback-2", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityExceeded, refused.Code);

        // Exact replay of the request still answers the same case inside the same transaction.
        var replay = await directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}rollback-1", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(first.ReturnRequestId, replay.ReturnRequestId);
        Assert.Equal(1, await db.ReturnRequests.AsNoTracking().CountAsync(x => x.SellerOrderId == sellerOrderId));
        _output.WriteLine($"TB272R1 refusal=refused replay={replay.ReturnRequestId}");

        await caller.RollbackAsync();
        AssertNoRolledBackRows(await CountTagRowsAsync());
    }

    // ------------------------------------------------------------------ target validation

    private static (bool Ok, string Reason) ValidateCanonicalTarget()
    {
        var raw = Connection;
        if (raw is null)
        {
            return (false, "TB272_CONNECTION is not set; this rollback-only relational proof did not run.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception)
        {
            return (false, "TB272_CONNECTION is not a valid Npgsql connection string; the proof did not run.");
        }

        if (!IsLoopback(builder.Host))
        {
            return (false, "TB272_CONNECTION does not address loopback; the canonical target was rejected.");
        }

        if (builder.Port != 5432 || !string.Equals(builder.Database, "tooba_alpha", StringComparison.Ordinal))
        {
            return (false, "TB272_CONNECTION does not address loopback port 5432 / tooba_alpha; the canonical target was rejected.");
        }

        try
        {
            using var connection = new NpgsqlConnection(raw);
            connection.Open();
            using var command = new NpgsqlCommand(
                "SELECT current_database(), inet_server_port(), host(inet_server_addr())", connection);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return (false, "The canonical target did not answer its own identity; the proof did not run.");
            }

            var database = reader.GetString(0);
            var port = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var address = reader.IsDBNull(2) ? "none" : reader.GetString(2);
            if (!string.Equals(database, "tooba_alpha", StringComparison.Ordinal) || port != 5432)
            {
                // Only the non-secret server identity is named, never the connection string or credentials.
                return (false,
                    $"The server identity (database={database}, port={port}) is not the canonical tooba_alpha database on 5432; the proof did not run.");
            }

            // The authoritative target identity is the CLIENT endpoint (loopback:5432, validated above) plus this
            // server-confirmed database name and server port. A published loopback port into the canonical container
            // reports the container's own interface address here (for example 172.18.0.2), which is why the address is
            // recorded as an observation instead of being required to be loopback.
            Console.WriteLine($"TB272R1 target database={database} serverPort={port} serverAddress={address}");
        }
        catch (Exception)
        {
            return (false, "The canonical tooba_alpha database is not reachable; the proof did not run.");
        }

        return (true, "canonical loopback tooba_alpha");
    }

    private static bool IsLoopback(string? host) =>
        host is not null
        && (host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.Ordinal));

    // ------------------------------------------------------------------ rollback-only bookkeeping

    /// <summary>Counts the rows this run could have committed. A rolled-back run must always answer zero.</summary>
    private async Task<long> CountTagRowsAsync()
    {
        await using var verify = CreateDb();
        return await verify.ReturnRequests.AsNoTracking()
            .LongCountAsync(x => x.IdempotencyKey.StartsWith(_runTag));
    }

    private void AssertNoRolledBackRows(long leftover)
    {
        _output.WriteLine($"TB272R1 rollback-only tag={_runTag} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------------------ owning-boundary fixtures

    private sealed record RequestRow(
        Guid RequestId,
        Guid SellerOrderId,
        Guid CheckoutId,
        Guid SellerPartyId,
        Guid RequestedByUserId,
        string IdempotencyKey,
        string Reason,
        string Status,
        decimal RefundAmount,
        string RefundDestination,
        string Origin);

    private static RequestRow NewRow(string key, string marker) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), key, marker,
            "Requested", 1000m, "OriginalPayment", "CustomerDelivered");

    private static async Task<RequestRow> InsertRequestAsync(ReturnsDbContext db, RequestRow row)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO returns.return_requests
                (return_request_id, seller_order_id, checkout_id, seller_party_id, requested_by_user_id,
                 idempotency_key, status, reason, currency, refund_amount, payment_id, created_at, updated_at,
                 origin, source_fulfillment_id, source_shipment_id, source_receipt_id, source_occurrence_key,
                 source_claim_id, obligation_closed_at, stock_settled_at, closure_phase, released_held_quantity,
                 refund_destination)
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, NULL, {10}, {11},
                    {12}, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'None', 0, {13})
            """,
            row.RequestId,
            row.SellerOrderId,
            row.CheckoutId,
            row.SellerPartyId,
            row.RequestedByUserId,
            row.IdempotencyKey,
            row.Status,
            row.Reason,
            "IRR",
            row.RefundAmount,
            DeliveredAt,
            DeliveredAt,
            row.Origin,
            row.RefundDestination);
        return row;
    }

    /// <summary>
    /// Controlled failure injection, used ONLY to reach the owning recovery branch that a rollback-only boundary
    /// otherwise cannot reach (a same-key winner is only visible to another connection after a commit). The raised
    /// exception carries the exact provider shape the real index produces — proven independently against PostgreSQL in
    /// <see cref="A_real_unique_violation_inside_a_caller_transaction_is_exactly_classified_and_savepoint_recoverable"/>.
    /// </summary>
    private sealed class ExactConflictInjector : SaveChangesInterceptor
    {
        private int _armed;

        public void Arm() => Interlocked.Exchange(ref _armed, 1);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _armed, 0) == 1)
            {
                var postgres = new PostgresException(
                    "duplicate key value violates unique constraint \"ix_return_requests_idempotency_key\"",
                    "ERROR",
                    "ERROR",
                    PostgresErrorCodes.UniqueViolation,
                    constraintName: ReturnCreationSerialization.IdempotencyUniqueIndex,
                    tableName: ReturnCreationSerialization.IdempotencyTable,
                    schemaName: ReturnCreationSerialization.IdempotencySchema);
                throw new DbUpdateException("tb272r1 injected idempotency collision", postgres);
            }

            return ValueTask.FromResult(result);
        }
    }

    private static ReturnsDbContext CreateDb(SaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, ReturnsDbContext.Schema, typeof(ReturnsDbContext));
        if (interceptor is not null)
        {
            options.AddInterceptors(interceptor);
        }

        return new ReturnsDbContext(options.Options);
    }

    private static CreateReturnCommand Command(
        Guid sellerOrderId,
        Guid actorUserId,
        string idempotencyKey,
        IReadOnlyList<ReturnLineCommand> items) =>
        new(sellerOrderId, actorUserId, idempotencyKey, "دلیل مرجوعی", items);

    private static ReturnDirectory BuildDirectory(
        ReturnsDbContext db,
        IOrderReturnReader orders,
        IFulfillmentReturnReader fulfillments)
    {
        var clock = new SystemUtcClock();
        return new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orders,
            new ReturnEligibilityEvaluator(orders, fulfillments, db, clock),
            new FixedPaymentReader(),
            new RecordingRefundGateway(),
            new RecordingWallet(),
            new RecordingInventory(),
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());
    }

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid customerUserId,
        decimal quantity,
        Guid[] lineIds,
        Guid? checkoutId = null,
        Guid? sellerPartyId = null) =>
        new(
            sellerOrderId,
            checkoutId ?? Guid.NewGuid(),
            sellerPartyId ?? Guid.NewGuid(),
            customerUserId,
            true,
            "IRR",
            lineIds
                .Select(x => new OrderReturnLineSnapshot(x, quantity, 1000m, "IRR", null, true, 7))
                .ToArray());

    private static FulfillmentReturnEligibilitySnapshot BuildFulfillment(
        Guid sellerOrderId,
        params (Guid LineId, decimal Delivered)[] lines) =>
        new(
            sellerOrderId,
            lines.ToDictionary(x => x.LineId, x => x.Delivered),
            DeliveredAt,
            lines.ToDictionary(x => x.LineId, _ => DeliveredAt),
            lines.Select(x => new LineDeliverySlice(x.LineId, x.Delivered, DeliveredAt)).ToArray());

    private sealed class MapOrderReader(IReadOnlyList<OrderReturnContextSnapshot> orders) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(orders.FirstOrDefault(x => x.SellerOrderId == sellerOrderId));
    }

    private sealed class MapFulfillmentReader(IReadOnlyList<FulfillmentReturnEligibilitySnapshot> fulfillments)
        : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(fulfillments.FirstOrDefault(x => x.SellerOrderId == sellerOrderId));

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, DateTimeOffset?> map = fulfillments
                .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
                .ToDictionary(x => x.SellerOrderId, x => x.LastDeliveredAt);
            return Task.FromResult(map);
        }
    }

    private sealed class FixedPaymentReader : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid payment,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1000m, "IRR", "Succeeded"));

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(Guid.NewGuid(), checkoutId, 1000m, "IRR", "Succeeded"));
    }

    private sealed class RecordingRefundGateway : IPaymentRefundGateway
    {
        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new GatewayRefundResult(true, "tb272r1-rel", null));
    }

    private sealed class RecordingWallet : IWalletRefundCreditPort
    {
        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletRefundCreditResultDto(amount, false));
    }

    private sealed class RecordingInventory : IReturnInventoryGateway
    {
        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
