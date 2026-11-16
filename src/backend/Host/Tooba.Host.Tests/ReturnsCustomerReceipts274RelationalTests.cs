using Microsoft.EntityFrameworkCore;
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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-274 on REAL PostgreSQL, under a STRICTLY ROLLBACK-ONLY boundary.
/// <para>
/// The customer receipt is a persisted physical fact whose value is the cumulative coverage of the recorded lines, so
/// the additive tables, their owning foreign keys, the check constraints and the cumulative over-receiving rule can
/// only be proven against the real relational schema. Every fact therefore stages its own case inside a transaction it
/// OWNS and rolls back; no row of this suite is ever committed.
/// </para>
/// <para>
/// The target is validated BEFORE any write: it must address loopback PostgreSQL port 5432 and database
/// <c>tooba_alpha</c>, and the server itself must confirm that identity read-only. A missing or non-canonical target
/// produces a visible framework SKIP (never a silent pass) and no connection is opened against it.
/// </para>
/// <para>
/// NOT demonstrated here (declared, not papered over): no real PSP, Wallet or Inventory money/stock moves, because the
/// physical receipt itself must move none; the receipt gate is proven against the real stored coverage while the
/// money/restock branches are proven by the controlled-ports suite.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class ReturnsCustomerReceipts274RelationalTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T08:00:00Z");

    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);

    private static string _canonicalIdentity = "(not validated)";

    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb274-rel-{Guid.NewGuid():N}-";

    public ReturnsCustomerReceipts274RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        (Environment.GetEnvironmentVariable("TB274_CONNECTION") is { Length: > 0 } direct ? direct
            : Environment.GetEnvironmentVariable("TB273R1_CONNECTION")) is { Length: > 0 } raw
            ? raw.Trim()
            : null;

    // ------------------------------------------------------------------ A: the stored physical fact

    [SkippableFact]
    public async Task A_receipt_rows_accumulate_cumulatively_and_never_exceed_the_requested_quantity()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, returnItemId) = await StageRequestedCaseAsync(db, $"{_runTag}coverage", quantity: 3m);

        var first = await directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-1", (returnItemId, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None);
        var partial = Assert.IsType<ReturnReceivingSummary>(first.Receiving);
        Assert.False(partial.IsComplete);
        Assert.Equal(1m, partial.Received);
        Assert.Equal(2m, partial.Remaining);

        // The rows really landed on the additive schema, attached to their own case.
        Assert.Equal(1, await db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == returnRequestId));
        Assert.Equal(1, await db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == returnRequestId));

        var second = await directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-2", (returnItemId, 2m, ReturnItemCondition.Usable)),
            CancellationToken.None);
        var complete = Assert.IsType<ReturnReceivingSummary>(second.Receiving);
        Assert.True(complete.IsComplete);
        Assert.True(complete.IsCoherent);
        Assert.Equal(3m, complete.Received);

        // An exact replay of the SAME occurrence answers the stored receipt and writes nothing.
        await directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-2", (returnItemId, 2m, ReturnItemCondition.Usable)),
            CancellationToken.None);
        Assert.Equal(2, await db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == returnRequestId));
        Assert.Equal(2, await db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == returnRequestId));

        // A further delivery would exceed what the customer was asked to return: refused with zero new rows.
        var over = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-3", (returnItemId, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.ReceiptOverReceived, over.Code);
        Assert.Equal(2, await db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == returnRequestId));

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine(
            $"TB274-A cumulative coverage tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    [SkippableFact]
    public async Task A_mixed_conditions_are_stored_on_distinct_lines_of_the_same_requested_line()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, returnItemId) = await StageRequestedCaseAsync(db, $"{_runTag}mixed", quantity: 2m);

        var recorded = await directory.RecordReceiptAsync(
            Receipt(
                returnRequestId,
                "delivery-1",
                (returnItemId, 1m, ReturnItemCondition.Usable),
                (returnItemId, 1m, ReturnItemCondition.Damaged)),
            CancellationToken.None);

        var receiving = Assert.IsType<ReturnReceivingSummary>(recorded.Receiving);
        Assert.True(receiving.IsComplete);
        Assert.Equal(1m, receiving.Usable);
        Assert.Equal(1m, receiving.Damaged);

        // Exactly one requested line still exists; the inspection lives on the receipt's own lines.
        Assert.Equal(1, await db.ReturnItems.CountAsync(x => x.ReturnRequestId == returnRequestId));
        var storedConditions = await db.ReturnReceiptLines.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => x.Condition)
            .ToListAsync();
        Assert.Equal(
            new[] { nameof(ReturnItemCondition.Damaged), nameof(ReturnItemCondition.Usable) },
            storedConditions.Select(x => x.ToString()).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // The same (line, condition) twice inside ONE occurrence is not a receipt intent.
        var duplicate = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RecordReceiptAsync(
            Receipt(
                returnRequestId,
                "delivery-2",
                (returnItemId, 1m, ReturnItemCondition.Usable),
                (returnItemId, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.ReceiptLineInvalid, duplicate.Code);
        Assert.Equal(1, await db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == returnRequestId));

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274-A mixed conditions tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------------------ B: the shared receiving gate

    [SkippableFact]
    public async Task B_approval_over_a_partial_receipt_is_refused_before_any_decision_attempt_or_money()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, returnItemId) = await StageRequestedCaseAsync(db, $"{_runTag}gate", quantity: 3m);
        await directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-1", (returnItemId, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.ApproveAsync(
            new ApproveReturnCommand(returnRequestId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal(ReturnsErrorCodes.ReceivingIncomplete, refused.Code);
        Assert.Empty(Gateway.Calls);
        Assert.Equal(0, await db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == returnRequestId));
        Assert.Equal(0, await db.RefundAttempts.CountAsync(x => x.ReturnRequestId == returnRequestId));
        var stored = await db.ReturnRequests.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId);
        Assert.Equal(ReturnRequestStatus.Requested, stored.Status);

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274-B receiving gate tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------------------ C: the rejection reason on the winning decision

    [SkippableFact]
    public async Task C_the_winning_rejection_stores_its_trimmed_reason_and_a_changed_reason_is_a_typed_conflict()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, _) = await StageRequestedCaseAsync(db, $"{_runTag}reason", quantity: 1m);
        var actor = Guid.NewGuid();

        await directory.RejectAsync(
            new RejectReturnCommand(returnRequestId, actor, Guid.NewGuid(), "  عیب واقعی کالا  "),
            CancellationToken.None);

        var stored = await db.ReturnDecisions.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId);
        Assert.Equal("Reject", stored.Decision);
        Assert.Equal("عیب واقعی کالا", stored.Reason);
        var rejected = await db.ReturnRequests.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId);
        Assert.Equal(ReturnRequestStatus.Rejected, rejected.Status);

        // The same statement replays the SAME durable decision: no second row, no second effect.
        await directory.RejectAsync(
            new RejectReturnCommand(returnRequestId, actor, Guid.NewGuid(), "عیب واقعی کالا"),
            CancellationToken.None);
        Assert.Equal(1, await db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == returnRequestId));

        // A different statement under the SAME winning decision is refused with zero effect.
        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RejectAsync(
            new RejectReturnCommand(returnRequestId, actor, Guid.NewGuid(), "دلیل دیگری"),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.DecisionReasonConflict, conflict.Code);
        Assert.Equal(
            "عیب واقعی کالا",
            (await db.ReturnDecisions.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId)).Reason);

        // The opposite decision can never win afterwards.
        var alreadyRejected = await Assert.ThrowsAsync<ContractOperationException>(() => directory.ApproveAsync(
            new ApproveReturnCommand(returnRequestId, actor, Guid.NewGuid()),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.AlreadyRejected, alreadyRejected.Code);
        Assert.Equal(0, await db.RefundAttempts.CountAsync(x => x.ReturnRequestId == returnRequestId));

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274-C rejection reason tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    [SkippableFact]
    public async Task C_a_rejection_without_a_meaningful_reason_writes_nothing()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, _) = await StageRequestedCaseAsync(db, $"{_runTag}noreason", quantity: 1m);

        foreach (var reason in new[] { null, "   " })
        {
            var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RejectAsync(
                new RejectReturnCommand(returnRequestId, Guid.NewGuid(), Guid.NewGuid(), reason),
                CancellationToken.None));
            Assert.Equal(ReturnsErrorCodes.RejectionReasonRequired, refused.Code);
        }

        Assert.Equal(0, await db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == returnRequestId));
        Assert.Equal(
            ReturnRequestStatus.Requested,
            (await db.ReturnRequests.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId)).Status);

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274-C missing reason tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------------------ D: decided cases never accept a new receipt

    [SkippableFact]
    public async Task D_a_decided_case_refuses_a_new_physical_receipt()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        var directory = BuildDirectory(db);

        await using var caller = await db.Database.BeginTransactionAsync();

        var (returnRequestId, returnItemId) = await StageRequestedCaseAsync(db, $"{_runTag}decided", quantity: 1m);
        await directory.RejectAsync(
            new RejectReturnCommand(returnRequestId, Guid.NewGuid(), Guid.NewGuid(), "مشتری انصراف داد"),
            CancellationToken.None);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RecordReceiptAsync(
            Receipt(returnRequestId, "delivery-1", (returnItemId, 1m, ReturnItemCondition.Usable)),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.ReceiptStateInvalid, refused.Code);
        Assert.Equal(0, await db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == returnRequestId));

        await caller.RollbackAsync();

        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB274-D decided case tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------------------ rollback-only staging

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
    /// Stages ONE ordinary delivered-customer case in <c>Requested</c> inside the caller's own transaction. Nothing is
    /// committed, so the case, its line and every receipt of this suite disappear with the rollback.
    /// </summary>
    private static async Task<(Guid ReturnRequestId, Guid ReturnItemId)> StageRequestedCaseAsync(
        ReturnsDbContext db,
        string idempotencyKey,
        decimal quantity)
    {
        var returnRequestId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            idempotencyKey,
            "دلیل مرجوعی",
            "IRR",
            [(orderLineId, quantity, 1000m, (Guid?)null)],
            Now);

        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        await db.SaveChangesAsync();

        return (returnRequestId, request.Items.Single().ReturnItemId);
    }

    private async Task<long> CountTagRowsAsync()
    {
        await using var verify = CreateDb();
        return await verify.ReturnRequests.AsNoTracking()
            .LongCountAsync(x => x.IdempotencyKey.StartsWith(_runTag));
    }

    // ------------------------------------------------------------------ canonical target

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

    // ------------------------------------------------------------------ owning-boundary wiring

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
            new CountingWallet(),            new CountingInventory(),
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
            return Task.FromResult(new GatewayRefundResult(true, "tb274-rel", null));
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
