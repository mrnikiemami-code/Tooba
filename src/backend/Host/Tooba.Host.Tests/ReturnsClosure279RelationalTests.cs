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
/// TB-279-R1 (A, D) on REAL PostgreSQL, under a STRICTLY ROLLBACK-ONLY boundary.
/// <para>
/// These facts exist because the shared payment budget and the atomic decision transition are DATABASE facts: the
/// InMemory provider cannot carry a transaction, cannot hold an advisory lock and cannot make an <c>UPDATE</c> +
/// <c>INSERT</c> move together. Each fact stages its own evidence inside a caller-owned transaction that the test
/// rolls back; nothing is committed, nothing is deleted afterwards, and the canonical database is re-read on a
/// separate connection at the end to prove no task row survived.
/// </para>
/// <para>
/// What is proven relationally: (1) the payment budget is ONE transaction-scoped advisory key per payment, so a
/// second session really waits for the first reservation instead of independently passing the ceiling; (2) that budget
/// counts the Pending/Succeeded obligations of EVERY Returns case of that payment and of no other payment; (3) the
/// version-fenced approval refuses a stale review with ZERO writes and, when it is lawful, moves the decision, the case
/// status and the frozen approved amount in the caller's ONE transaction.
/// </para>
/// <para>
/// NOT demonstrated here (declared, not papered over): a COMMITTED two-writer race — the effectful first-refund intent
/// refuses a caller-owned transaction before any effect, and a competing row is invisible to another session until it
/// commits, which this boundary forbids. No provider, PSP, Wallet or Inventory call is made anywhere.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class ReturnsClosure279RelationalTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-18T09:30:00Z");

    private static readonly Guid SellerPartyId = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);

    private static string _canonicalIdentity = "(not validated)";

    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb279r1-rel-{Guid.NewGuid():N}-";

    public ReturnsClosure279RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        (Environment.GetEnvironmentVariable("TB279R1_CONNECTION") is { Length: > 0 } direct ? direct
            : Environment.GetEnvironmentVariable("TB273R1_CONNECTION")) is { Length: > 0 } raw
            ? raw.Trim()
            : null;

    // ------------------------------------------------------------------ A: one shared payment budget

    [SkippableFact]
    public async Task The_payment_budget_counts_every_case_of_that_payment_and_no_other_payment()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();

        var paymentId = Guid.NewGuid();
        var otherPaymentId = Guid.NewGuid();

        // Case A of the payment holds a PENDING 60 (its money outcome is unknown, so it still spends the ceiling).
        await StageCaseAsync(db, $"{_runTag}budget-a", paymentId, pendingAmount: 60m);
        // A SECOND case (another seller order) of the SAME payment is only approved: it has no attempt yet.
        await StageCaseAsync(db, $"{_runTag}budget-b", paymentId, pendingAmount: 0m);
        // A different payment's pending attempt must never be counted against this one.
        await StageCaseAsync(db, $"{_runTag}budget-c", otherPaymentId, pendingAmount: 100m);

        var counted = await CountBudgetAsync(db, paymentId);
        var otherCounted = await CountBudgetAsync(db, otherPaymentId);

        Assert.Equal(60m, counted);
        Assert.Equal(100m, otherCounted);

        // The shared ceiling of the owning payment is therefore 100 - 60 = 40 for ANY further case of it.
        Assert.True(counted + 60m > 100m, "a second 60 reservation of payment 100 must exceed the shared ceiling");
        Assert.True(counted + 40m <= 100m, "the exact remainder of payment 100 must still be reservable");
        _output.WriteLine(
            $"TB279R1-A payment={paymentId} counted={counted} otherPayment={otherPaymentId} counted={otherCounted}");

        await caller.RollbackAsync();
        AssertNoRolledBackRows(await CountTagRowsAsync());
    }

    [SkippableFact]
    public async Task A_second_session_really_waits_for_the_payment_budget_lock()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var paymentId = Guid.NewGuid();
        var otherPaymentId = Guid.NewGuid();

        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();

        // A foreign session owns THIS payment's budget lock first.
        await using var holder = new NpgsqlConnection(Connection);
        await holder.OpenAsync();
        await using var held = await holder.BeginTransactionAsync();
        await using (var lockCommand = new NpgsqlCommand(
            $"SELECT pg_advisory_xact_lock({ReturnBudgetSerialization.AdvisoryLockKey(paymentId)})",
            holder,
            held))
        {
            await lockCommand.ExecuteNonQueryAsync();
        }

        // The owning Returns reservation protocol takes the SAME key, so it must block, not pass.
        var pending = ReturnBudgetSerialization.AcquireAsync(db.Database, paymentId, CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        Assert.False(pending.IsCompleted, "the payment budget was acquired while another session still held it");

        // A DIFFERENT payment is independent: same namespace, different key, no contention.
        await AssertOtherPaymentIsIndependentAsync(otherPaymentId);

        // Releasing the foreign lock releases the reservation step.
        await held.RollbackAsync();
        await pending.WaitAsync(TimeSpan.FromSeconds(30));

        // Both locks are transaction-scoped: the caller's rollback ends the owning one.
        await caller.RollbackAsync();

        AssertNoRolledBackRows(await CountTagRowsAsync());
        _output.WriteLine($"TB279R1-A2 payment={paymentId} waited=true independent={otherPaymentId}");
    }

    // ------------------------------------------------------------------ D: one atomic, version-fenced decision

    [SkippableFact]
    public async Task A_stale_reviewed_version_writes_nothing_and_a_lawful_approval_moves_every_fact_together()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateDb();
        await using var caller = await db.Database.BeginTransactionAsync();

        var paymentId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var (returnRequestId, _) = await StageReceivableCaseAsync(db, $"{_runTag}decision", checkoutId, paymentId, 2m, 1000m, partialResolution: true);
        var directory = BuildDirectory(db, new FixedPaymentReader(paymentId, checkoutId, 100m));

        // The operator reviewed effective version 5, which is not what the case carries: refuse with zero writes.
        var stale = await Assert.ThrowsAsync<ContractOperationException>(() => directory.ApproveAsync(
            new ApproveReturnCommand(returnRequestId, Guid.NewGuid(), SellerPartyId, RefundDestination.OriginalPayment, 5),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.CorrectionVersionStale, stale.Code);

        var afterStale = await db.ReturnRequests.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId);
        Assert.Equal(ReturnRequestStatus.Requested, afterStale.Status);
        Assert.Null(afterStale.PaymentId);
        Assert.Null(afterStale.ApprovedAmount);
        Assert.Equal(0, await db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == returnRequestId));

        // The reviewed version 0 is the effective one: decision + status + frozen amount move in this ONE transaction.
        var approved = await directory.ApproveAsync(
            new ApproveReturnCommand(returnRequestId, Guid.NewGuid(), SellerPartyId, RefundDestination.OriginalPayment, 0),
            CancellationToken.None);

        Assert.Equal(ReturnRequestStatus.Approved, approved.Status);
        // Approval is a DECISION only: no attempt, no money, no stock.
        Assert.Empty(await db.RefundAttempts.Where(x => x.ReturnRequestId == returnRequestId).ToListAsync());

        var decided = await db.ReturnRequests.AsNoTracking().SingleAsync(x => x.ReturnRequestId == returnRequestId);
        Assert.Equal(ReturnRequestStatus.Approved, decided.Status);
        Assert.Equal(paymentId, decided.PaymentId);
        Assert.Equal(1000m, decided.ApprovedAmount);            // 1 accepted × the STORED 1000 unit price
        Assert.Equal(2000m, decided.RefundAmount);              // the requested amount stays immutable
        Assert.Equal(1, await db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == returnRequestId));

        // The caller still owns the transaction: the facts above are visible only inside it and vanish on rollback.
        Assert.NotNull(db.Database.CurrentTransaction);
        await caller.RollbackAsync();

        AssertNoRolledBackRows(await CountTagRowsAsync());
        _output.WriteLine(
            $"TB279R1-D request={returnRequestId} stale=zero-write approved=1000 requested=2000 attempts=0");
    }

    // ------------------------------------------------------------------ staging

    /// <summary>Stages one APPROVED case of a payment with an optional Pending/Succeeded obligation.</summary>
    private async Task StageCaseAsync(
        ReturnsDbContext db,
        string idempotencyKey,
        Guid paymentId,
        decimal pendingAmount)    {
        var request = ReturnRequest.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SellerPartyId,
            Guid.NewGuid(),
            idempotencyKey,
            "دلیل مرجوعی",
            "IRR",
            [(Guid.NewGuid(), 1m, 1000m, (Guid?)null)],
            Now,
            RefundDestination.OriginalPayment);

        request.Approve(paymentId, Now, RefundDestination.OriginalPayment, pendingAmount > 0m ? pendingAmount : null);
        if (pendingAmount > 0m)
        {
            request.MarkRefundProcessing(Now);
            db.RefundAttempts.Add(request.BeginRefundAttempt(
                Guid.NewGuid(), paymentId, $"refund-{request.ReturnRequestId:N}", Now));
        }

        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        db.ReturnDecisions.Add(new ReturnDecisionRecord
        {
            ReturnRequestId = request.ReturnRequestId,
            Decision = ReturnRecoveryPolicy.ApprovedDecision,
            DecidedByUserId = Guid.NewGuid(),
            DecidedAt = Now,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Stages one REQUESTED case that is lawfully receivable: every requested unit arrived usable.</summary>
    private async Task<(Guid ReturnRequestId, Guid LineId)> StageReceivableCaseAsync(
        ReturnsDbContext db,
        string idempotencyKey,
        Guid checkoutId,
        Guid paymentId,
        decimal quantity,
        decimal unitPrice,
        bool partialResolution = false)
    {
        var returnRequestId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            checkoutId,
            SellerPartyId,
            Guid.NewGuid(),
            idempotencyKey,
            "دلیل مرجوعی",
            "IRR",
            [(lineId, quantity, unitPrice, (Guid?)null)],
            Now,
            RefundDestination.OriginalPayment);

        var receipt = ReturnReceipt.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            returnRequestId,
            $"tb279r1-delivery-{returnRequestId:N}",
            Guid.NewGuid(),
            Now,
            [(request.Items.Single().ReturnItemId, quantity, ReturnItemCondition.Usable)]);

        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        db.ReturnReceipts.Add(receipt);
        db.ReturnReceiptLines.AddRange(receipt.Lines);

        if (partialResolution)
        {
            // The reviewed split: ONE of the two received units is accepted, the other is declined and stays owed.
            db.ReturnItemResolutions.Add(new ReturnItemResolution
            {
                ReturnRequestId = returnRequestId,
                ReturnItemId = request.Items.Single().ReturnItemId,
                Version = 1,
                AcceptedQuantity = 1m,
                RejectedQuantity = 1m,
                AcceptedUsableQuantity = 1m,
                AcceptedDamagedQuantity = 0m,
                RejectedReceivedUsableQuantity = 1m,
                RejectedReceivedDamagedQuantity = 0m,
                RejectedUnreceivedQuantity = 0m,
                Reason = "یک عدد پذیرفته شد و یک عدد رد شد.",
                ActorUserId = Guid.NewGuid(),
                OccurrenceKey = $"tb279r1-resolution-{returnRequestId:N}",
                RecordedAt = Now,
            });
        }

        await db.SaveChangesAsync();
        return (returnRequestId, lineId);
    }

    /// <summary>The EXACT budget read the reservation protocol performs: this payment's Pending + Succeeded rows.</summary>
    private static async Task<decimal> CountBudgetAsync(ReturnsDbContext db, Guid paymentId)
    {
        var committed = await db.RefundAttempts.AsNoTracking()
            .Where(x => x.PaymentId == paymentId
                && (x.Status == RefundAttemptStatus.Pending || x.Status == RefundAttemptStatus.Succeeded))
            .Select(x => x.Amount)
            .ToListAsync();
        return committed.Sum();
    }

    /// <summary>A second payment's key must not be blocked by the first payment's holder.</summary>
    private static async Task AssertOtherPaymentIsIndependentAsync(Guid otherPaymentId)
    {
        await using var session = new NpgsqlConnection(Connection);
        await session.OpenAsync();
        await using var transaction = await session.BeginTransactionAsync();
        await using (var timeout = new NpgsqlCommand("SET LOCAL lock_timeout = '2000ms'", session, transaction))
        {
            await timeout.ExecuteNonQueryAsync();
        }

        await using var acquire = new NpgsqlCommand(
            $"SELECT pg_advisory_xact_lock({ReturnBudgetSerialization.AdvisoryLockKey(otherPaymentId)})",
            session,
            transaction);
        await acquire.ExecuteNonQueryAsync();
        await transaction.RollbackAsync();
    }

    private async Task<long> CountTagRowsAsync()
    {
        await using var verify = CreateDb();
        return await verify.ReturnRequests.AsNoTracking()
            .LongCountAsync(x => x.IdempotencyKey.StartsWith(_runTag));
    }

    private static void AssertNoRolledBackRows(long leftover) => Assert.Equal(0L, leftover);

    // ------------------------------------------------------------------ canonical target

    private static (bool Ok, string Reason) ValidateCanonicalTarget()
    {
        var raw = Connection;
        if (raw is null)
        {
            return (false, "TB279R1_CONNECTION is not set; this rollback-only relational proof did not run.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception)
        {
            return (false, "TB279R1_CONNECTION is not a valid Npgsql connection string; the proof did not run.");
        }

        if (!IsLoopback(builder.Host))
        {
            return (false, "TB279R1_CONNECTION does not address loopback; the canonical target was rejected.");
        }

        if (builder.Port != 5432 || !string.Equals(builder.Database, "tooba_alpha", StringComparison.Ordinal))
        {
            return (false, "TB279R1_CONNECTION does not address loopback port 5432 / tooba_alpha; the proof did not run.");
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

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, Connection!, ReturnsDbContext.Schema, typeof(ReturnsDbContext));
        return new ReturnsDbContext(options.Options);
    }

    private static ReturnDirectory BuildDirectory(ReturnsDbContext db, IPaymentReturnReader payments)
    {
        var clock = new FixedClock();
        var orders = new EmptyOrderReader();
        var fulfillments = new EmptyFulfillmentReader();
        return new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orders,
            new ReturnEligibilityEvaluator(orders, fulfillments, db, clock),
            payments,
            new CountingGateway(),
            new CountingWallet(),
            new CountingInventory(),
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = Now;
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

    private sealed class FixedPaymentReader(Guid paymentId, Guid checkoutId, decimal amount) : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid payment,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                payment == paymentId ? new PaymentReturnSnapshot(paymentId, checkoutId, amount, "IRR", "Succeeded") : null);

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid requestedCheckoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                requestedCheckoutId == checkoutId
                    ? new PaymentReturnSnapshot(paymentId, checkoutId, amount, "IRR", "Succeeded")
                    : null);
    }

    private sealed class CountingGateway : IPaymentRefundGateway
    {
        public int Calls { get; private set; }

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new GatewayRefundResult(true, "tb279r1-rel", null));
        }
    }

    private sealed class CountingWallet : IWalletRefundCreditPort
    {
        public int Calls { get; private set; }

        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new WalletRefundCreditResultDto(amount, false));
        }
    }

    private sealed class CountingInventory : IReturnInventoryGateway
    {
        public int Calls { get; private set; }

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
