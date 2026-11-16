using System.Transactions;
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
/// TB-273-R1 (B, D) on REAL PostgreSQL, under a STRICTLY ROLLBACK-ONLY boundary.
/// <para>
/// The caller-transaction precondition is a relational fact: the InMemory provider cannot carry a transaction at
/// all, so a probe there proves nothing. Each fact therefore stages the exact stored evidence inside a transaction it
/// OWNS and then rolls back, calls the REAL owning directory (<see cref="ReturnDirectory.RecoverAsync"/>) and asserts
/// that the effectful intent was refused with the stable typed reason BEFORE any gateway/Wallet/Inventory call or
/// recovery write — and that the caller's transaction, rows and tracked entities were left untouched.
/// </para>
/// <para>
/// The target is validated BEFORE any write: it must address loopback PostgreSQL port 5432 and database
/// <c>tooba_alpha</c>, and the server itself must confirm that identity read-only. A missing or non-canonical target
/// produces a visible framework SKIP (never a silent pass) and no connection is opened against it.
/// </para>
/// <para>
/// NOT demonstrated here (declared, not papered over): the outcome coordination (<c>FOR UPDATE</c> + reload + apply)
/// cannot be exercised without a COMMITTED real case row, which this rollback-only boundary forbids; and no provider,
/// PSP, Wallet or Inventory operation is performed anywhere. The controlled-ports suite covers those branches.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class ReturnsRecovery273RelationalTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T08:00:00Z");

    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);

    /// <summary>
    /// The identity the canonical server confirmed about itself on the last validation (database:port/address), kept
    /// only so the evidence lines can state WHERE the rolled-back proof ran.
    /// </summary>
    private static string _canonicalIdentity = "(not validated)";

    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb273r1-rel-{Guid.NewGuid():N}-";

    public ReturnsRecovery273RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        (Environment.GetEnvironmentVariable("TB273R1_CONNECTION") is { Length: > 0 } direct ? direct
            : Environment.GetEnvironmentVariable("TB272_CONNECTION")) is { Length: > 0 } raw
            ? raw.Trim()
            : null;

    // ------------------------------------------------------------------ B: a caller-owned transaction is refused

    [SkippableFact]
    public async Task A_caller_owned_transaction_refuses_effectful_recovery_before_any_effect()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var gateway = new CountingGateway();
        var wallet = new CountingWallet();
        var inventory = new CountingInventory();

        await using var db = CreateDb();
        var directory = BuildDirectory(db, gateway, wallet, inventory);

        ContractOperationException? refusal = null;
        await using (var caller = await db.Database.BeginTransactionAsync())
        {
            var (returnRequestId, attemptId) = await StageApprovedCaseAsync(db, $"{_runTag}caller");

            refusal = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RecoverAsync(
                new RecoverReturnCommand(
                    returnRequestId, Guid.NewGuid(), ReturnRecoveryKind.ResumePendingRefund, attemptId),
                CancellationToken.None));

            // The caller's transaction is STILL the owner: it was never committed, rolled back or disposed by recovery.
            Assert.NotNull(db.Database.CurrentTransaction);

            // Nothing was sent and nothing was written by the refused intent.
            Assert.Equal(0, gateway.Calls);
            Assert.Equal(0, wallet.Calls);
            Assert.Equal(0, inventory.Calls);
            var storedAttempt = await db.RefundAttempts.AsNoTracking().SingleAsync();
            Assert.Equal(RefundAttemptStatus.Pending, storedAttempt.Status);
            Assert.Equal(ReturnRequestStatus.RefundProcessing, (await db.ReturnRequests.AsNoTracking().SingleAsync()).Status);

            // The caller's tracked entities keep their instance, state and values.
            var tracked = db.ChangeTracker.Entries<RefundAttempt>().Single();
            Assert.Equal(EntityState.Unchanged, tracked.State);
            Assert.Equal(RefundAttemptStatus.Pending, tracked.Entity.Status);

            await caller.RollbackAsync();
        }

        Assert.Equal(ReturnsErrorCodes.RecoveryTransactionOwned, refusal!.Code);
        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB273R1-B caller-owned refusal tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    [SkippableFact]
    public async Task An_ambient_transaction_scope_refuses_effectful_recovery_before_any_effect()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        var gateway = new CountingGateway();
        var wallet = new CountingWallet();
        var inventory = new CountingInventory();

        await using var db = CreateDb();
        var directory = BuildDirectory(db, gateway, wallet, inventory);

        ContractOperationException? refusal = null;
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            var (returnRequestId, attemptId) = await StageApprovedCaseAsync(db, $"{_runTag}ambient");

            refusal = await Assert.ThrowsAsync<ContractOperationException>(() => directory.RecoverAsync(
                new RecoverReturnCommand(
                    returnRequestId, Guid.NewGuid(), ReturnRecoveryKind.ResumePendingRefund, attemptId),
                CancellationToken.None));

            Assert.Equal(0, gateway.Calls);
            Assert.Equal(0, wallet.Calls);
            Assert.Equal(0, inventory.Calls);

            // Deliberately NOT completed: disposal rolls the ambient scope back.
        }

        Assert.Equal(ReturnsErrorCodes.RecoveryTransactionOwned, refusal!.Code);
        var leftover = await CountTagRowsAsync();
        _output.WriteLine($"TB273R1-B ambient refusal tag={_runTag} target={_canonicalIdentity} committedRows={leftover}");
        Assert.Equal(0L, leftover);
    }

    // ------------------------------------------------------------------ rollback-only staging

    /// <summary>
    /// Stages the exact durable evidence an approval records — requested case, APPROVE decision, refunding case and
    /// one PENDING bound attempt — inside the caller's own transaction. Nothing is committed.
    /// </summary>
    private static async Task<(Guid ReturnRequestId, Guid AttemptId)> StageApprovedCaseAsync(
        ReturnsDbContext db,
        string idempotencyKey)
    {
        var returnRequestId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

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
            [(lineId, 1m, 1000m, (Guid?)null)],
            Now);

        request.Approve(paymentId, Now);
        request.MarkRefundProcessing(Now);
        var attempt = request.BeginRefundAttempt(
            Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);

        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        db.RefundAttempts.Add(attempt);
        db.ReturnDecisions.Add(new ReturnDecisionRecord
        {
            ReturnRequestId = returnRequestId,
            Decision = ReturnRecoveryPolicy.ApprovedDecision,
            DecidedByUserId = Guid.NewGuid(),
            DecidedAt = Now,
        });

        await db.SaveChangesAsync();
        return (returnRequestId, attempt.RefundAttemptId);
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
            return (false, "TB273R1_CONNECTION is not set; this rollback-only relational proof did not run.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception)
        {
            return (false, "TB273R1_CONNECTION is not a valid Npgsql connection string; the proof did not run.");
        }

        if (!IsLoopback(builder.Host))
        {
            return (false, "TB273R1_CONNECTION does not address loopback; the canonical target was rejected.");
        }

        if (builder.Port != 5432 || !string.Equals(builder.Database, "tooba_alpha", StringComparison.Ordinal))
        {
            return (false, "TB273R1_CONNECTION does not address loopback port 5432 / tooba_alpha; the canonical target was rejected.");
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

            // The canonical single-store deployment answers on a private/container address, so the server-side
            // address is required to be loopback or private — never a public endpoint — while the CLIENT target
            // above is already pinned to loopback.
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

    /// <summary>RFC1918 / IPv6 ULA: the private container/bridge address the canonical store answers on.</summary>
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
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, ReturnsDbContext.Schema, typeof(ReturnsDbContext));
        return new ReturnsDbContext(options.Options);
    }

    private static ReturnDirectory BuildDirectory(
        ReturnsDbContext db,
        CountingGateway gateway,
        CountingWallet wallet,
        CountingInventory inventory)
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
            gateway,
            wallet,
            inventory,
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
        public int Calls { get; private set; }

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new GatewayRefundResult(true, "tb273r1-rel", null));
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
