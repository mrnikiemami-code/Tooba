using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
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
/// TB-P0-CHECKOUT-FINISH-PAYMENT-021 Priority A focused proof: a reused Payment idempotency key must
/// describe the very same payment operation. Cross-checkout or cross-method reuse fails closed with the
/// registered <c>payment.idempotency.conflict</c> BEFORE any gateway initiation/retry, while identical
/// method replays (including canonical alias/casing) and legitimate same-method retries still work.
/// </summary>
public sealed class PaymentIdempotencyConflictGuardTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-13T09:00:00Z");

    [Fact]
    public async Task Same_key_same_checkout_same_method_replays_without_new_attempt_or_gateway_call()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("same"), Now);
        var registry = new RecordingGatewayRegistry();

        var result = await CreateDirectory(db, registry, new PermissiveReader()).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("same"), "fake"),
            CancellationToken.None);

        Assert.Equal(payment.PaymentId, result.PaymentId);
        Assert.Equal("fake", result.ProviderCode);
        Assert.Equal(PaymentStatus.Pending, result.Status);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(1, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
    }

    [Fact]
    public async Task Replay_matches_the_canonical_provider_alias_case_insensitively()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("casing"), Now);
        var registry = new RecordingGatewayRegistry();

        var result = await CreateDirectory(db, registry, new PermissiveReader()).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("casing"), "FAKE"),
            CancellationToken.None);

        Assert.Equal(payment.PaymentId, result.PaymentId);
        Assert.Equal(0, registry.InitiationCount);
    }

    [Fact]
    public async Task Same_key_with_a_different_provider_fails_closed_without_reaching_the_gateway()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("method"), Now);
        var registry = new RecordingGatewayRegistry();

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateDirectory(db, registry, new PermissiveReader()).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("method"), "fake-fail"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.IdempotencyConflict, fault.Code);
        Assert.True(PaymentErrorCodes.IsKnown(fault.Code));
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(1, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
    }

    [Fact]
    public async Task Failed_prior_with_a_different_provider_is_rejected_before_the_gateway_retry()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("failed-method"), Now);
        payment.ApplyVerifiedFailure(payment.Attempts.Single().AttemptId, "GATEWAY_REJECTED", Now.AddMinutes(1));
        await db.SaveChangesAsync(CancellationToken.None);
        var registry = new RecordingGatewayRegistry();

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateDirectory(db, registry, new PermissiveReader()).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("failed-method"), "fake-fail"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.IdempotencyConflict, fault.Code);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(1, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
    }

    [Fact]
    public async Task Same_key_and_same_method_retry_after_failure_is_still_allowed()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("legit-retry"), Now);
        payment.ApplyVerifiedFailure(payment.Attempts.Single().AttemptId, "GATEWAY_REJECTED", Now.AddMinutes(1));
        await db.SaveChangesAsync(CancellationToken.None);
        var registry = new RecordingGatewayRegistry();

        var result = await CreateDirectory(db, registry, new PermissiveReader()).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("legit-retry"), "fake"),
            CancellationToken.None);

        Assert.Equal(payment.PaymentId, result.PaymentId);
        Assert.Equal(1, registry.InitiationCount);
        Assert.Equal(2, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
    }

    [Fact]
    public async Task Same_key_on_another_checkout_is_rejected_even_for_the_same_actor_and_method()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var otherCheckoutId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var payment = SeedPayment(db, checkoutId, "fake", Key("cross-checkout"), Now);
        var registry = new RecordingGatewayRegistry();

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateDirectory(db, registry, new PermissiveReader()).InitiateAsync(
                new InitiatePaymentCommand(otherCheckoutId, actor, null, Key("cross-checkout"), "fake"),
                CancellationToken.None));

        Assert.Equal(PaymentErrorCodes.IdempotencyConflict, fault.Code);
        Assert.Equal(0, registry.InitiationCount);
        Assert.Equal(1, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
        Assert.Equal(1, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task Conflict_check_never_precedes_actor_visibility()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        SeedPayment(db, checkoutId, "fake", Key("ordered"), Now);
        var registry = new RecordingGatewayRegistry();

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateDirectory(db, registry, new DenyingReader()).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, Key("ordered"), "fake-fail"),
                CancellationToken.None));

        Assert.Equal("payment.access.order_identity_required", fault.Code);
        Assert.Equal(0, registry.InitiationCount);
    }

    [Fact]
    public void New_code_is_registered_with_conflict_semantics_and_localization_key()
    {
        var catalog = new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor(), new PaymentErrorCatalogContributor()]);

        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.IdempotencyConflict));
        Assert.True(catalog.TryGet(PaymentErrorCodes.IdempotencyConflict, out var descriptor));
        Assert.Equal(409, descriptor.HttpStatus);
        Assert.Equal(PaymentErrorCodes.IdempotencyConflict, descriptor.LocalizationKey);
    }

    private static string Key(string suffix) => "idem-021-" + suffix;

    private static PaymentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase("payment-idem-021-" + Guid.NewGuid().ToString("N"))
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
        payment.RecordInitiation(Guid.NewGuid(), "ref-021", now.AddSeconds(1));

        db.Payments.Add(payment);
        db.Allocations.AddRange(payment.Allocations);
        db.Attempts.AddRange(payment.Attempts);
        db.SaveChanges();
        return payment;
    }

    private static PaymentDirectory CreateDirectory(
        PaymentDbContext db,
        IPaymentGatewayRegistry registry,
        IPayableCheckoutReader reader) =>
        new(
            db,
            new OpenPaymentUseCaseGuard(),
            reader,
            registry,
            new PaymentGatewayActorContext(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

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
                []));
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
