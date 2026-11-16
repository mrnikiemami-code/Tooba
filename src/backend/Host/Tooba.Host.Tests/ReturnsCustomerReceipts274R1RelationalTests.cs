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
/// TB-274-R1 on REAL PostgreSQL, under a STRICTLY ROLLBACK-ONLY boundary.
/// <para>
/// The R1 repair moved two facts into the STORE itself — the positivity/defined-inspection checks and the local
/// composite ownership of a receipt line — and narrowed the duplicate identity to ONE occurrence. Those are exactly
/// the facts a controlled in-memory provider cannot prove, so each one is exercised here against the canonical
/// <c>tooba_alpha</c> schema inside a transaction this suite OWNS and rolls back.
/// </para>
/// <para>
/// The target is validated BEFORE any write: loopback PostgreSQL on port 5432, database <c>tooba_alpha</c>, and the
/// server itself must confirm that identity read-only. A missing or non-canonical target produces a visible SKIP, never
/// a silent pass. No money, wallet, inventory or provider effect is claimed here.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class ReturnsCustomerReceipts274R1RelationalTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T09:00:00Z");

    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);

    private static string _canonicalIdentity = "(not validated)";

    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb274r1-rel-{Guid.NewGuid():N}-";

    public ReturnsCustomerReceipts274R1RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        (Environment.GetEnvironmentVariable("TB274_CONNECTION") is { Length: > 0 } direct ? direct
            : Environment.GetEnvironmentVariable("TB273R1_CONNECTION")) is { Length: > 0 } raw
            ? raw.Trim()
            : null;

    // ------------------------------------------------------- A: cumulative occurrences are legitimately separate rows

    [SkippableFact]
    public async Task A_two_occurrences_of_the_same_line_and_condition_are_two_legal_rows_and_accumulate()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, returnItemId, _) = await StageRequestedCaseAsync(db, $"{_runTag}cumulative", 3m);

        var first = await directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-1", (returnItemId, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None);
        Assert.Equal(1m, Assert.IsType<ReturnReceivingSummary>(first.Receiving).Received);

        // The SAME requested line, the SAME inspection, delivered AGAIN under its own occurrence identity. The stored
        // unique identity is (receipt, item, condition) — not (case, item, condition) — so this is lawful coverage.
        var second = await directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-2", (returnItemId, 2m, ReturnItemCondition.Usable)),
            CancellationToken.None);

        var receiving = Assert.IsType<ReturnReceivingSummary>(second.Receiving);
        Assert.True(receiving.IsCoherent);
        Assert.True(receiving.IsComplete);
        Assert.Equal(3m, receiving.Received);
        Assert.Equal(3m, receiving.Usable);
        Assert.Equal(0m, receiving.Remaining);

        var rows = await db.ReturnReceiptLines.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => new { x.ReturnReceiptId, x.Quantity, x.Condition })
            .ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows.Select(x => x.ReturnReceiptId).Distinct().Count());
        Assert.All(rows, x => Assert.Equal(ReturnItemCondition.Usable, x.Condition));
        Assert.Equal(3m, rows.Sum(x => x.Quantity));

        // The real index definition names the OCCURRENCE as part of its identity.
        var indexDefinition = await db.Database
            .SqlQueryRaw<string>(
                "SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'returns' AND indexname = 'ix_customer_return_receipt_lines_receipt_item_condition'")
            .SingleAsync();
        Assert.Contains("return_receipt_id", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("return_item_id", indexDefinition, StringComparison.Ordinal);
        Assert.Contains("condition", indexDefinition, StringComparison.Ordinal);
        Assert.True(
            !indexDefinition.Contains("return_request_id", StringComparison.Ordinal),
            $"the unique identity must not be case-wide: {indexDefinition}");

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274R1-A cumulative occurrences tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------- B: the store itself refuses malformed lines

    [SkippableFact]
    public async Task B_the_stored_checks_refuse_a_nonpositive_quantity_and_an_undefined_inspection()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, returnItemId, _) = await StageRequestedCaseAsync(db, $"{_runTag}checks", 2m);
        await directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-1", (returnItemId, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None);
        var receiptId = await db.ReturnReceipts.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => x.ReturnReceiptId)
            .SingleAsync();

        // A zero quantity can never be persisted, whatever a future writer tries.
        var zero = await InsertLineAsync(
            db,
            returnRequestId,
            receiptId,
            returnItemId,
            0m,
            "Usable",
            "tb274r1_zero");
        Assert.Equal("23514", zero.SqlState);
        Assert.Equal("ck_customer_return_receipt_lines_quantity_positive", zero.ConstraintName);

        // Neither can an inspection token that is neither usable nor damaged.
        var undefined = await InsertLineAsync(
            db,
            returnRequestId,
            receiptId,
            returnItemId,
            1m,
            "GivenBack",
            "tb274r1_undefined");
        Assert.Equal("23514", undefined.SqlState);
        Assert.Equal("ck_customer_return_receipt_lines_condition_defined", undefined.ConstraintName);

        // Both refusals are attributed to the STORE: nothing was left behind by them.
        Assert.Equal(
            1,
            await db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == returnRequestId));

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274R1-B stored checks tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    [SkippableFact]
    public async Task B_a_line_naming_another_cases_receipt_is_refused_by_the_local_composite_foreign_key()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        var caller = await db.Database.BeginTransactionAsync();

        var (caseA, itemA, _) = await StageRequestedCaseAsync(db, $"{_runTag}owner-a", 2m);
        var (caseB, itemB, _) = await StageRequestedCaseAsync(db, $"{_runTag}owner-b", 2m);

        await directory.RecordReceiptAsync(
            Receipt(caseA, "delivery-a", (itemA, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None);
        await directory.RecordReceiptAsync(
            Receipt(caseB, "delivery-b", (itemB, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None);

        var headerA = await db.ReturnReceipts.AsNoTracking()
            .Where(x => x.ReturnRequestId == caseA)
            .Select(x => x.ReturnReceiptId)
            .SingleAsync();
        var headerB = await db.ReturnReceipts.AsNoTracking()
            .Where(x => x.ReturnRequestId == caseB)
            .Select(x => x.ReturnReceiptId)
            .SingleAsync();

        // Case A's header + case B's line: a borrowed receipt header can never be stored as if it belonged to A.
        var borrowedHeader = await InsertLineAsync(
            db,
            caseA,
            headerB,
            itemA,
            1m,
            "Damaged",
            "tb274r1_borrowed_header");
        Assert.Equal("23503", borrowedHeader.SqlState);
        Assert.Equal(
            "fk_customer_return_receipt_lines_receipts_owning_case",
            borrowedHeader.ConstraintName);

        // Case A's own header + case B's requested item: the same local ownership refuses the foreign item.
        var foreignItem = await InsertLineAsync(
            db,
            caseA,
            headerA,
            itemB,
            1m,
            "Damaged",
            "tb274r1_foreign_item");
        Assert.Equal("23503", foreignItem.SqlState);
        Assert.Equal(
            "fk_customer_return_receipt_lines_return_items_owning_case",
            foreignItem.ConstraintName);

        // The legitimate rows of both cases are untouched by the two refusals.
        Assert.Equal(1, await db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == caseA));
        Assert.Equal(1, await db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == caseB));

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274R1-B composite ownership tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------- B: the owning directory refuses a foreign seller scope

    [SkippableFact]
    public async Task B_a_foreign_seller_scope_cannot_write_a_decision_on_the_real_store()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, _, sellerPartyId) = await StageRequestedCaseAsync(db, $"{_runTag}scope", 1m);

        var foreign = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RejectAsync(
            new RejectReturnCommand(returnRequestId, Guid.NewGuid(), Guid.NewGuid(), "دلیل"),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.Missing, foreign.Code);
        Assert.Equal(0, await db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == returnRequestId));

        // The owning scope is accepted on the SAME stored case, so the refusal above is the scope and not the case.
        await directory.RejectAsync(
            new RejectReturnCommand(returnRequestId, Guid.NewGuid(), sellerPartyId, "دلیل مالک"),
            CancellationToken.None);
        var decision = await db.ReturnDecisions.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId);
        Assert.Equal("Reject", decision.Decision);
        Assert.Equal("دلیل مالک", decision.Reason);

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274R1-B owning scope tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------- rollback-only staging

    private static RecordReturnReceiptCommand Receipt(
        Guid returnRequestId,
        string occurrenceKey,
        params (Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)[] lines) =>
        new(
            returnRequestId,
            Guid.NewGuid(),
            occurrenceKey,
            lines.Select(x => new CustomerReceiptLine(x.ReturnItemId, x.Quantity, x.Condition)).ToArray());

    /// <summary>
    /// Attempts one raw line insert inside its OWN savepoint, so a store-level refusal does not poison the caller's
    /// transaction and the exact provider error can be inspected. The savepoint is always rolled back.
    /// </summary>
    private static async Task<PostgresException> InsertLineAsync(
        ReturnsDbContext db,
        Guid returnRequestId,
        Guid returnReceiptId,
        Guid returnItemId,
        decimal quantity,
        string condition,
        string savePoint)
    {
        await db.Database.ExecuteSqlRawAsync($"SAVEPOINT {savePoint}");
        try
        {
            await db.Database.ExecuteSqlAsync(
                $"""
                 INSERT INTO returns.customer_return_receipt_lines
                     (return_receipt_line_id, return_receipt_id, return_request_id, return_item_id, quantity, condition)
                 VALUES ({Guid.NewGuid()}, {returnReceiptId}, {returnRequestId}, {returnItemId}, {quantity}, {condition})
                 """);
        }
        catch (PostgresException exception)
        {
            await db.Database.ExecuteSqlRawAsync($"ROLLBACK TO SAVEPOINT {savePoint}");
            await db.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {savePoint}");
            return exception;
        }

        await db.Database.ExecuteSqlRawAsync($"ROLLBACK TO SAVEPOINT {savePoint}");
        await db.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {savePoint}");
        throw new Xunit.Sdk.XunitException(
            $"the store accepted a line it must refuse (quantity={quantity}, condition='{condition}')");
    }

    /// <summary>
    /// Stages ONE ordinary delivered-customer case in <c>Requested</c> inside the caller's own transaction. Nothing is
    /// committed, so the case, its line and every receipt of this suite disappear with the rollback.
    /// </summary>
    private static async Task<(Guid ReturnRequestId, Guid ReturnItemId, Guid SellerPartyId)> StageRequestedCaseAsync(
        ReturnsDbContext db,
        string idempotencyKey,
        decimal quantity)
    {
        var returnRequestId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sellerPartyId,
            Guid.NewGuid(),
            idempotencyKey,
            "دلیل مرجوعی",
            "IRR",
            [(orderLineId, quantity, 1000m, (Guid?)null)],
            Now);

        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        await db.SaveChangesAsync();

        return (returnRequestId, request.Items.Single().ReturnItemId, sellerPartyId);
    }

    private async Task<long> CountTagRowsAsync()
    {
        await using var verify = CreateDb();
        return await verify.ReturnRequests.AsNoTracking()
            .LongCountAsync(x => x.IdempotencyKey.StartsWith(_runTag));
    }

    // ------------------------------------------------------- canonical target

    private static (bool Ok, string Reason) ValidateCanonicalTarget()
    {
        var raw = Connection;
        if (raw is null)
        {
            return (false, "TB274_CONNECTION is not set; this rollback-only relational proof did not run.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception)
        {
            return (false, "TB274_CONNECTION is not a valid Npgsql connection string; the proof did not run.");
        }

        if (!IsLoopback(builder.Host))
        {
            return (false, "TB274_CONNECTION does not address loopback; the canonical target was rejected.");
        }

        if (builder.Port != 5432 || !string.Equals(builder.Database, "tooba_alpha", StringComparison.Ordinal))
        {
            return (false, "TB274_CONNECTION does not address loopback port 5432 / tooba_alpha; the canonical target was rejected.");
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
            _canonicalIdentity = $"{database}:{port}/{address}";
            if (!string.Equals(database, "tooba_alpha", StringComparison.Ordinal) || port != 5432)
            {
                return (false, $"The server identifies itself as {database}:{port}; the canonical target was rejected.");
            }

            if (!IsLoopback(address) && !IsPrivateAddress(address))
            {
                return (false, $"The server address {address} is neither loopback nor private; the canonical target was rejected.");
            }
        }
        catch (Exception)
        {
            return (false, "The canonical target could not be verified read-only; the proof did not run.");
        }

        return (true, string.Empty);
    }

    private static bool IsLoopback(string? host) =>
        !string.IsNullOrWhiteSpace(host)
        && (string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.Ordinal));

    private static bool IsPrivateAddress(string? host)
    {
        if (!System.Net.IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
    }

    // ------------------------------------------------------- owning-boundary wiring

    private static readonly CountingGateway Gateway = new();

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, ReturnsDbContext.Schema, typeof(ReturnsDbContext));
        return new ReturnsDbContext(options.Options);
    }

    private static ReturnDirectory BuildDirectory(ReturnsDbContext db)
    {
        var clock = new SystemUtcClock();
        var orders = new EmptyOrderReader();
        var fulfillments = new EmptyFulfillmentReader();
        return new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orders,
            new ReturnEligibilityEvaluator(orders, fulfillments, db, clock),
            new EmptyPaymentReader(),
            Gateway,
            new CountingWallet(),
            new CountingInventory(),
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());
    }

    private sealed class EmptyOrderReader : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderReturnContextSnapshot?>(null);
    }

    private sealed class EmptyFulfillmentReader : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentReturnEligibilitySnapshot?>(null);

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTimeOffset?>>(
                new Dictionary<Guid, DateTimeOffset?>());
    }

    private sealed class EmptyPaymentReader : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid payment,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(null);

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(null);
    }

    private sealed class CountingGateway : IPaymentRefundGateway
    {
        public List<Guid> Calls { get; } = [];

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add(paymentId);
            return Task.FromResult(new GatewayRefundResult(true, "tb274r1-rel", null));
        }
    }

    private sealed class CountingWallet : IWalletRefundCreditPort
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

    private sealed class CountingInventory : IReturnInventoryGateway
    {
        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
