using Microsoft.EntityFrameworkCore;
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
/// TB-272 (A + D) on REAL PostgreSQL: the ordinary creation decision is one critical section per canonical seller
/// order, and the one unique index on <c>returns.return_requests.idempotency_key</c> is the only collision that turns
/// into the typed <c>return.idempotency_conflict</c>.
/// </summary>
/// <remarks>
/// Gated by <c>TB272_CONNECTION</c>; the target must be the ONE canonical database (127.0.0.1:5432/tooba_alpha).
/// The Order/Fulfillment/payment/wallet seams are controlled in-memory readers (no provider is called, no money moves,
/// no stock changes); every row this class writes is task-labelled <c>tb272-rel-…</c> and is deleted again by the
/// exact idempotency-key prefix of this run. No user order, case, item or attempt row is read, written or deleted.
/// </remarks>
[Collection("PostgresSerial")]
public sealed class ReturnsCreateIntegrity272RelationalTests
{
    private static readonly DateTimeOffset DeliveredAt = DateTimeOffset.UtcNow.AddDays(-1);

    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb272-rel-{Guid.NewGuid():N}-";

    public ReturnsCreateIntegrity272RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB272_CONNECTION") is { Length: > 0 } raw ? raw : null;

    // ------------------------------------------------------------------ D: the advisory lock is the serialization

    [Fact]
    public async Task The_creation_decision_waits_for_the_seller_order_advisory_lock()
    {
        if (Connection is null)
        {
            return;
        }

        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var customer = Guid.NewGuid();
        var orders = new MapOrderReader([BuildOrder(sellerOrderId, customer, 2m, [lineId])]);
        var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        await using var db = CreateDb();
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
        _output.WriteLine($"TB272-D lock order={sellerOrderId} request={snapshot.ReturnRequestId}");

        await CleanupAsync();
    }

    // ------------------------------------------------------------------ D: no over-allocation under concurrency

    [Fact]
    public async Task Concurrent_creations_never_over_allocate_the_last_returnable_unit()
    {
        if (Connection is null)
        {
            return;
        }

        for (var round = 1; round <= 3; round++)
        {
            var sellerOrderId = Guid.NewGuid();
            var lineId = Guid.NewGuid();
            var customer = Guid.NewGuid();
            var orders = new MapOrderReader([BuildOrder(sellerOrderId, customer, 3m, [lineId])]);
            var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 3m))]);

            // Two of the three delivered units are already requested and COMMITTED, so exactly ONE unit is left.
            await using (var seedDb = CreateDb())
            {
                var seed = await BuildDirectory(seedDb, orders, fulfillments).CreateAsync(
                    Command(sellerOrderId, customer, $"{_runTag}r{round}-seed", [new ReturnLineCommand(lineId, 2m)]),
                    CancellationToken.None);
                Assert.Equal(2000m, seed.RefundAmount);
            }

            await using var leftDb = CreateDb();
            await using var rightDb = CreateDb();
            var left = BuildDirectory(leftDb, orders, fulfillments);
            var right = BuildDirectory(rightDb, orders, fulfillments);

            // Two DIFFERENT keys, submitted at the same time, each asking for the same single remaining unit. Without
            // the per-order critical section both would read remaining = 1 and both would insert.
            var results = await Task.WhenAll(
                AttemptAsync(left, sellerOrderId, customer, $"{_runTag}r{round}-left", lineId),
                AttemptAsync(right, sellerOrderId, customer, $"{_runTag}r{round}-right", lineId));

            var accepted = results.Count(x => x.RequestId is not null);
            var refused = results.Where(x => x.RequestId is null).ToArray();
            Assert.Equal(1, accepted);
            Assert.Single(refused);
            Assert.Equal(ReturnsErrorCodes.QuantityExceeded, refused[0].Code);

            await using var verifyDb = CreateDb();
            var requests = await verifyDb.ReturnRequests.AsNoTracking()
                .Where(x => x.SellerOrderId == sellerOrderId)
                .Select(x => x.ReturnRequestId)
                .ToListAsync();
            var requestedUnits = await verifyDb.ReturnItems.AsNoTracking()
                .Where(x => requests.Contains(x.ReturnRequestId))
                .SumAsync(x => x.Quantity);

            Assert.Equal(2, requests.Count);
            Assert.Equal(3m, requestedUnits);
            _output.WriteLine($"TB272-D round={round} order={sellerOrderId} accepted={accepted} requestedUnits={requestedUnits}");
        }

        await CleanupAsync();
    }

    // ------------------------------------------------------------------ A: the real unique index is the collision

    [Fact]
    public async Task The_same_key_on_two_orders_collides_on_the_real_unique_index_and_conflicts()
    {
        if (Connection is null)
        {
            return;
        }

        var customer = Guid.NewGuid();
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var key = $"{_runTag}collision";
        var orders = new MapOrderReader([
            BuildOrder(orderA, customer, 2m, [lineA]),
            BuildOrder(orderB, customer, 2m, [lineB]),
        ]);
        var fulfillments = new MapFulfillmentReader([
            BuildFulfillment(orderA, (lineA, 2m)),
            BuildFulfillment(orderB, (lineB, 2m)),
        ]);

        // Order A's creation runs inside a caller-owned transaction, so its row stays UNCOMMITTED. Order B has a
        // DIFFERENT advisory-lock key (another order), so it is not serialized against A: its read sees no row and its
        // insert must collide with the real unique index once A commits.
        await using var dbA = CreateDb();
        await using var transactionA = await dbA.Database.BeginTransactionAsync();
        var createdA = await BuildDirectory(dbA, orders, fulfillments).CreateAsync(
            Command(orderA, customer, key, [new ReturnLineCommand(lineA, 1m)]),
            CancellationToken.None);
        Assert.Equal(orderA, createdA.SellerOrderId);

        await using var dbB = CreateDb();
        var pendingB = BuildDirectory(dbB, orders, fulfillments).CreateAsync(
            Command(orderB, customer, key, [new ReturnLineCommand(lineB, 1m)]),
            CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        Assert.False(pendingB.IsCompleted);

        await transactionA.CommitAsync();

        var conflict = await Assert.ThrowsAsync<ContractOperationException>(
            () => pendingB.WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, conflict.Code);

        await using var verifyDb = CreateDb();
        var stored = await verifyDb.ReturnRequests.AsNoTracking()
            .Where(x => x.IdempotencyKey == key)
            .ToListAsync();
        Assert.Single(stored);
        Assert.Equal(createdA.ReturnRequestId, stored[0].ReturnRequestId);
        Assert.Equal(orderA, stored[0].SellerOrderId);
        Assert.Equal(
            0,
            await verifyDb.ReturnRequests.AsNoTracking().CountAsync(x => x.SellerOrderId == orderB));
        _output.WriteLine($"TB272-A uniqueIndex winner={stored[0].ReturnRequestId} loser={ReturnsErrorCodes.IdempotencyConflict}");

        await CleanupAsync();
    }

    // ------------------------------------------------------------------ A: only the real key index is a collision

    [Fact]
    public async Task Only_the_idempotency_index_is_classified_as_a_key_collision()
    {
        if (Connection is null)
        {
            return;
        }

        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var orders = new MapOrderReader([BuildOrder(sellerOrderId, customer, 2m, [lineId])]);
        var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        await using var db = CreateDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
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
                    (return_item_id, return_request_id, order_line_id, quantity, unit_price_snapshot, currency)
                VALUES ({0}, {1}, {2}, {3}, {4}, {5})
                """,
                item.ReturnItemId,
                item.ReturnRequestId,
                item.OrderLineId,
                item.Quantity,
                item.UnitPriceSnapshot,
                item.Currency);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        var postgres = failure as PostgresException ?? failure?.InnerException as PostgresException;
        Assert.NotNull(postgres);
        Assert.Equal("pk_return_items", postgres!.ConstraintName);
        Assert.False(ReturnCreationSerialization.IsIdempotencyConflict(new DbUpdateException("test", postgres)));

        await transaction.RollbackAsync();
        await CleanupAsync();
    }

    // ------------------------------------------------------------------ rollback + release on the refusal path

    [Fact]
    public async Task A_refused_creation_leaves_no_row_and_releases_its_lock()
    {
        if (Connection is null)
        {
            return;
        }

        var customer = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var orders = new MapOrderReader([BuildOrder(sellerOrderId, customer, 1m, [lineId])]);
        var fulfillments = new MapFulfillmentReader([BuildFulfillment(sellerOrderId, (lineId, 1m))]);

        await using var db = CreateDb();
        var directory = BuildDirectory(db, orders, fulfillments);
        var first = await directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}rollback-1", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);

        // Over-asking the exhausted line is refused inside the critical section; that transaction must roll back and
        // release the lock, so the next writer on the SAME order is not blocked by the refusal.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}rollback-2", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityExceeded, refused.Code);

        // Exact replay of the committed request still answers the same case.
        var replay = await directory.CreateAsync(
            Command(sellerOrderId, customer, $"{_runTag}rollback-1", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(first.ReturnRequestId, replay.ReturnRequestId);

        await using var verifyDb = CreateDb();
        Assert.Equal(1, await verifyDb.ReturnRequests.AsNoTracking().CountAsync(x => x.SellerOrderId == sellerOrderId));
        _output.WriteLine($"TB272 refusal=rolled-back replay={replay.ReturnRequestId}");

        await CleanupAsync();
    }

    // ------------------------------------------------------------------ fixtures

    private async Task CleanupAsync()
    {
        if (Connection is null)
        {
            return;
        }

        await using var db = CreateDb();
        var owned = await db.ReturnRequests
            .Where(x => x.IdempotencyKey.StartsWith(_runTag))
            .Select(x => x.ReturnRequestId)
            .ToListAsync();
        if (owned.Count == 0)
        {
            _output.WriteLine($"TB272 cleanup tag={_runTag} rows=0");
            return;
        }

        await db.ReturnItems.Where(x => owned.Contains(x.ReturnRequestId)).ExecuteDeleteAsync();
        await db.RefundAttempts.Where(x => owned.Contains(x.ReturnRequestId)).ExecuteDeleteAsync();
        await db.ReturnRequests.Where(x => owned.Contains(x.ReturnRequestId)).ExecuteDeleteAsync();
        _output.WriteLine($"TB272 cleanup tag={_runTag} rows={owned.Count}");
    }

    private static async Task<(Guid? RequestId, string? Code)> AttemptAsync(
        ReturnDirectory directory,
        Guid sellerOrderId,
        Guid customer,
        string key,
        Guid lineId)
    {
        try
        {
            var snapshot = await directory.CreateAsync(
                Command(sellerOrderId, customer, key, [new ReturnLineCommand(lineId, 1m)]),
                CancellationToken.None);
            return (snapshot.ReturnRequestId, null);
        }
        catch (ContractOperationException exception)
        {
            return (null, exception.Code);
        }
    }

    private static CreateReturnCommand Command(
        Guid sellerOrderId,
        Guid actorUserId,
        string idempotencyKey,
        IReadOnlyList<ReturnLineCommand> items) =>
        new(sellerOrderId, actorUserId, idempotencyKey, "TB272 relational", items);

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

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, ReturnsDbContext.Schema, typeof(ReturnsDbContext));
        return new ReturnsDbContext(options.Options);
    }

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid customerUserId,
        decimal quantity,
        Guid[] lineIds) =>
        new(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
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
            Task.FromResult(new GatewayRefundResult(true, "tb272-rel", null));
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
