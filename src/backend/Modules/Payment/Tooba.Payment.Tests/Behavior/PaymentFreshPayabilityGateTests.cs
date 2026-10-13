using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Contracts.Payments;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Checkout;
using Tooba.Payment.Contracts.Errors;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Endpoints.Errors;
using Tooba.Payment.Infrastructure.Directories;
using Tooba.Payment.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// TB-P0-PAYMENT-FRESH-GATE-WIRING-025 focused proof for the mandatory fresh-payment gate decorator.
/// The Order-side truth is a typed fake; every non-Payable verdict must refuse BEFORE any provider call
/// or payment/attempt row, while identical-key replay, the 021 conflict order and actor visibility stay
/// exactly as <c>PaymentDirectory</c> owns them.
/// </summary>
public sealed class PaymentFreshPayabilityGateTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-13T09:00:00Z");

    [Fact]
    public async Task Payable_fresh_checkout_reaches_the_gateway_once_and_persists_one_payment()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.Payable);

        var result = await CreateGate(db, registry, new PermissiveReader(), fresh).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("fresh-ok"), "fake"),
            CancellationToken.None);

        Assert.Equal(PaymentStatus.Pending, result.Status);
        Assert.Equal(1, registry.InitiationCount);
        Assert.Equal(1, await db.Payments.CountAsync());
        Assert.Equal(1, fresh.Calls);
        Assert.Equal(checkoutId, fresh.LastCheckoutId);
    }

    [Theory]
    [InlineData(OrderFreshPaymentPayability.CheckoutUnavailable)]
    [InlineData(OrderFreshPaymentPayability.ReservationSetIncomplete)]
    [InlineData(OrderFreshPaymentPayability.CycleNotPayable)]
    [InlineData(OrderFreshPaymentPayability.RetryExhausted)]
    [InlineData(OrderFreshPaymentPayability.HoldUnavailable)]
    [InlineData(OrderFreshPaymentPayability.AuthorityUnavailable)]
    public async Task Every_non_payable_verdict_refuses_the_fresh_path_before_any_gateway_or_write(
        OrderFreshPaymentPayability outcome)
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var registry = new RecordingGatewayRegistry();

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateGate(db, registry, new PermissiveReader(), new FreshFake(outcome)).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("fresh-" + outcome), "fake"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.ReservationNotPayable, fault.Code);
        Assert.True(PaymentErrorCodes.IsKnown(fault.Code));
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(0, await db.Payments.CountAsync());
        Assert.Equal(0, await db.Attempts.CountAsync());
    }

    [Fact]
    public async Task Failed_prior_same_operation_is_gated_before_the_provider_retry()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("failed"), Now);
        payment.ApplyVerifiedFailure(payment.Attempts.Single().AttemptId, "GATEWAY_REJECTED", Now.AddMinutes(1));
        await db.SaveChangesAsync(CancellationToken.None);
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.CycleNotPayable);

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateGate(db, registry, new PermissiveReader(), fresh).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("failed"), "fake"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.ReservationNotPayable, fault.Code);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(1, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
        Assert.Equal(1, fresh.Calls);
    }

    [Fact]
    public async Task Failed_prior_same_operation_retries_when_the_cycle_is_payable()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("failed-ok"), Now);
        payment.ApplyVerifiedFailure(payment.Attempts.Single().AttemptId, "GATEWAY_REJECTED", Now.AddMinutes(1));
        await db.SaveChangesAsync(CancellationToken.None);
        var registry = new RecordingGatewayRegistry();

        var result = await CreateGate(db, registry, new PermissiveReader(), new FreshFake(OrderFreshPaymentPayability.Payable))
            .InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("failed-ok"), "fake"),
                CancellationToken.None);

        Assert.Equal(payment.PaymentId, result.PaymentId);
        Assert.Equal(1, registry.InitiationCount);
        Assert.Equal(2, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
    }

    [Fact]
    public async Task Pending_identical_key_replay_is_never_rechecked_against_the_cycle()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("pending-replay"), Now);
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.CycleNotPayable);

        var result = await CreateGate(db, registry, new PermissiveReader(), fresh).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("pending-replay"), "fake"),
            CancellationToken.None);

        Assert.Equal(payment.PaymentId, result.PaymentId);
        Assert.Equal(PaymentStatus.Pending, result.Status);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(0, fresh.Calls);
    }

    [Fact]
    public async Task Succeeded_payment_is_never_masked_by_the_reservation_verdict()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var succeeded = SeedPayment(db, checkoutId, "fake", Key("already"), Now);
        succeeded.ApplyVerifiedSuccess(succeeded.Attempts.Single().AttemptId, "txn-025-already", Now.AddMinutes(1));
        await db.SaveChangesAsync(CancellationToken.None);
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.CycleNotPayable);

        // A fresh key on a checkout that already succeeded: the inner directory stays the authority and
        // must still emit payment.already_succeeded, not the reservation verdict.
        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateGate(db, registry, new PermissiveReader(), fresh).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("already-second"), "fake"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.AlreadySucceeded, fault.Code);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(0, fresh.Calls);
    }

    [Fact]
    public async Task Cross_checkout_key_reuse_still_wins_over_the_reservation_verdict()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var otherCheckoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("cross"), Now);
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.CycleNotPayable);

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateGate(db, registry, new PermissiveReader(), fresh).InitiateAsync(
                new InitiatePaymentCommand(otherCheckoutId, Guid.NewGuid(), null, Key("cross"), "fake"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.IdempotencyConflict, fault.Code);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(1, await db.Payments.CountAsync(x => x.PaymentId == payment.PaymentId));
        Assert.Equal(0, fresh.Calls);
    }

    [Fact]
    public async Task Cross_method_key_reuse_still_wins_over_the_reservation_verdict()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        SeedPayment(db, checkoutId, "fake", Key("cross-method"), Now);
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.CycleNotPayable);

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateGate(db, registry, new PermissiveReader(), fresh).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("cross-method"), "fake-fail"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.IdempotencyConflict, fault.Code);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(0, fresh.Calls);
    }

    [Fact]
    public async Task Actor_visibility_still_precedes_the_reservation_verdict()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        SeedPayment(db, checkoutId, "fake", Key("visible"), Now);
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.CycleNotPayable);

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateGate(db, registry, new DenyingReader(), fresh).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("visible"), "fake"),
                CancellationToken.None));

        Assert.Equal("payment.access.order_identity_required", fault.Code);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(0, fresh.Calls);
    }

    [Fact]
    public async Task Gate_refuses_before_the_provider_is_even_resolved_for_an_unknown_checkout()
    {
        await using var db = CreateDb();
        var registry = new RecordingGatewayRegistry();
        var fresh = new FreshFake(OrderFreshPaymentPayability.CheckoutUnavailable);

        await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateGate(db, registry, new DenyingReader(), fresh).InitiateAsync(
                new InitiatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), null, Key("denied"), "fake"),
                CancellationToken.None));

        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(1, fresh.Calls);
    }

    [Fact]
    public async Task Non_initiate_members_are_delegated_unchanged()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("delegate"), Now);
        var registry = new RecordingGatewayRegistry();
        var gate = CreateGate(db, registry, new PermissiveReader(), new FreshFake(OrderFreshPaymentPayability.Payable));

        Assert.True(await gate.HasSucceededPaymentForCheckoutAsync(checkoutId, CancellationToken.None) == false);
        var snapshot = await gate.GetAsync(payment.PaymentId, Guid.NewGuid(), null, CancellationToken.None);
        Assert.NotNull(snapshot);
        Assert.Equal(payment.PaymentId, snapshot!.PaymentId);
    }

    [Fact]
    public async Task Cancellation_propagates_through_the_gate()
    {
        await using var db = CreateDb();
        var registry = new RecordingGatewayRegistry();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateGate(db, registry, new PermissiveReader(), new FreshFake(OrderFreshPaymentPayability.Payable))
                .InitiateAsync(
                    new InitiatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), null, Key("cancel"), "fake"),
                    cancelled.Token));
    }

    [Fact]
    public void New_code_is_registered_with_conflict_semantics_localization_and_known_guard()
    {
        var catalog = new ErrorDefinitionCatalog(
            [new FoundationErrorCatalogContributor(), new PaymentErrorCatalogContributor()]);

        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.ReservationNotPayable));
        Assert.True(catalog.TryGet(PaymentErrorCodes.ReservationNotPayable, out var descriptor));
        Assert.Equal(409, descriptor.HttpStatus);
        Assert.Equal(PaymentErrorCodes.ReservationNotPayable, descriptor.LocalizationKey);
    }

    [Fact]
    public void Gate_is_the_single_registered_money_entry_and_keeps_a_required_order_dependency()
    {
        var root = Repo();
        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs"));
        var directory = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs"));

        Assert.Contains("new PaymentDirectory(", module, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IPaymentDirectory>(sp => new FreshPaymentGateDirectory(", module, StringComparison.Ordinal);
        Assert.Contains("sp.GetRequiredService<IOrderPaymentFreshPayabilityPort>()", module, StringComparison.Ordinal);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(module, @"AddScoped<\s*IPaymentDirectory\b").Count);

        // The frozen certified directory stays untouched: the gate never lives inside it, and no
        // reservation verdict was bolted onto the historical read projection.
        Assert.DoesNotContain("ReservationNotPayable", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("GetFreshPaymentPayabilityAsync", directory, StringComparison.Ordinal);
    }

    private static string Key(string suffix) => "idem-025-" + suffix;

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repository root not found");
    }

    private static PaymentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase("payment-fresh-gate-025-" + Guid.NewGuid().ToString("N"))
            .Options);

    private static CustomerPayment SeedPayment(
        PaymentDbContext db,
        Guid checkoutId,
        string providerCode,
        string idempotencyKey,
        DateTimeOffset now)
    {
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            checkoutId,
            1000m,
            "IRR",
            providerCode,
            idempotencyKey,
            [(PaymentAllocationTargetKind.SellerOrder, Guid.NewGuid(), 1000m, Guid.NewGuid())],
            now);
        payment.RecordInitiation(Guid.NewGuid(), "ref-025", now.AddSeconds(1));

        db.Payments.Add(payment);
        db.Allocations.AddRange(payment.Allocations);
        db.Attempts.AddRange(payment.Attempts);
        db.SaveChanges();
        return payment;
    }

    private static IPaymentDirectory CreateGate(
        PaymentDbContext db,
        IPaymentGatewayRegistry registry,
        IPayableCheckoutReader reader,
        IOrderPaymentFreshPayabilityPort fresh) =>
        new FreshPaymentGateDirectory(
            new PaymentDirectory(
                db,
                new OpenPaymentUseCaseGuard(),
                reader,
                registry,
                new PaymentGatewayActorContext(),
                new SystemUtcClock(),
                new UuidV7IdGenerator()),
            db,
            reader,
            registry,
            fresh);

    private sealed class FreshFake(OrderFreshPaymentPayability outcome) : IOrderPaymentFreshPayabilityPort
    {
        public int Calls { get; private set; }

        public Guid? LastCheckoutId { get; private set; }

        public Task<OrderFreshPaymentPayabilityResult> GetFreshPaymentPayabilityAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            LastCheckoutId = checkoutId;
            return Task.FromResult(new OrderFreshPaymentPayabilityResult(
                outcome,
                checkoutId,
                ReservationCount: outcome == OrderFreshPaymentPayability.Payable ? 1 : 0,
                UnavailableReservationIds: [],
                CycleNumber: null,
                CycleExpiresAtUtc: null,
                RetryCountRemaining: null,
                EffectiveMaxCycles: null));
        }
    }

    private sealed class PermissiveReader : IPayableCheckoutReader
    {
        public Task<PayableCheckoutSnapshot?> GetPayableAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PayableCheckoutSnapshot?>(new PayableCheckoutSnapshot(
                checkoutId,
                OrderPaymentMode.OnlinePurchase,
                "IRR",
                [new PayableSellerOrderSnapshot(Guid.NewGuid(), 1000m, "IRR")]));
    }

    private sealed class DenyingReader : IPayableCheckoutReader
    {
        public Task<PayableCheckoutSnapshot?> GetPayableAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PayableCheckoutSnapshot?>(null);
    }

    private sealed class RecordingGatewayRegistry : IPaymentGatewayRegistry
    {
        private readonly Dictionary<string, RecordingGateway> _gateways =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["fake"] = new("fake"),
                ["fake-fail"] = new("fake-fail"),
            };

        public int InitiationCount => _gateways.Values.Sum(x => x.InitiationCount);

        public IPaymentGateway Resolve(string providerCode) =>
            _gateways.TryGetValue(providerCode.Trim(), out var gateway)
                ? gateway
                : throw new InvalidOperationException("payment.gateway.unknown");
    }

    private sealed class RecordingGateway(string providerCode) : IPaymentGateway
    {
        public string ProviderCode { get; } = providerCode;

        public int InitiationCount { get; private set; }

        public Task<GatewayInitiation> InitiateAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken)
        {
            _ = amount;
            _ = currency;
            InitiationCount++;
            return Task.FromResult(new GatewayInitiation(
                $"{ProviderCode}-{paymentId:N}",
                $"/payment/{ProviderCode}?ref={paymentId:N}",
                Now.AddMinutes(15)));
        }

        public Task<GatewayVerification> VerifyAsync(
            string providerRequestReference,
            bool callbackClaimsSuccess,
            CancellationToken cancellationToken)
        {
            _ = callbackClaimsSuccess;
            return Task.FromResult(new GatewayVerification(true, "txn-" + providerRequestReference, null));
        }
    }
}
