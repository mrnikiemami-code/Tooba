using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Contracts.Returns;
using Tooba.Payment.Infrastructure.Directories;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Providers;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// TB-TMAR-PAYMENT-PRECERT-HYGIENE-001 focused proof: typed fault boundaries only —
/// refund gateway, Wallet order-payment boundary, and the renamed contract bridge.
/// </summary>
public sealed class PaymentPrecertHygieneTests
{
    [Fact]
    public async Task FailClosed_refund_gateway_throws_typed_contract_fault_with_stable_code()
    {
        var gateway = new FailClosedPaymentRefundGateway();

        var ex = await Assert.ThrowsAsync<ContractOperationException>(() => gateway.RefundAsync(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "order-cancel-refund:x",
            CancellationToken.None));

        Assert.Equal("payment.refund.gateway.unconfigured", ex.Code);
    }

    [Fact]
    public async Task Order_cancel_refund_preserves_refund_pending_on_typed_unconfigured_code()
    {
        await using var db = CreateDb();
        var payment = SeedSucceededPayment(db);

        var admin = CreateAdminDirectory(db, new ThrowingRefundGateway("payment.refund.gateway.unconfigured"));
        await admin.CloseOrStartRefundForOrderCancelAsync(payment.CheckoutId, CancellationToken.None);

        var reloaded = await db.Payments.AsNoTracking().SingleAsync(x => x.PaymentId == payment.PaymentId);
        Assert.Equal(PaymentStatus.RefundPending, reloaded.Status);
    }

    [Fact]
    public async Task Order_cancel_refund_marks_refund_failed_from_typed_code_not_message_text()
    {
        await using var db = CreateDb();
        var payment = SeedSucceededPayment(db);

        var admin = CreateAdminDirectory(db, new ThrowingRefundGateway("payment.refund.gateway.declined"));
        await admin.CloseOrStartRefundForOrderCancelAsync(payment.CheckoutId, CancellationToken.None);

        var reloaded = await db.Payments.AsNoTracking().SingleAsync(x => x.PaymentId == payment.PaymentId);
        Assert.Equal(PaymentStatus.RefundFailed, reloaded.Status);
    }

    [Fact]
    public async Task Order_cancel_refund_leaves_state_untouched_for_unknown_exception()
    {
        await using var db = CreateDb();
        var payment = SeedSucceededPayment(db);

        var admin = CreateAdminDirectory(db, new ThrowingRefundGateway(null));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            admin.CloseOrStartRefundForOrderCancelAsync(payment.CheckoutId, CancellationToken.None));

        var reloaded = await db.Payments.AsNoTracking().SingleAsync(x => x.PaymentId == payment.PaymentId);
        Assert.Equal(PaymentStatus.RefundPending, reloaded.Status);
    }

    [Fact]
    public async Task Wallet_gateway_converts_typed_wallet_rejection_to_spend_rejected()
    {
        var actorId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var paymentId = Guid.Parse("01900000-0000-7000-8000-000000000901");
        var port = new RecordingWalletPort { SpendFaultCode = "wallet.rejected.2YXZiNis" };
        var actorCtx = new PaymentGatewayActorContext { ActorUserId = actorId };
        var gateway = new WalletPaymentGateway(port, actorCtx, new SystemUtcClock(), new ModuleCallTracer());
        var reference = WalletPaymentGateway.ComposeReference(paymentId, actorId, 50_000m, "IRR");

        var result = await gateway.VerifyAsync(reference, true, CancellationToken.None);

        Assert.False(result.VerifiedSuccess);
        Assert.Equal("WALLET_SPEND_REJECTED", result.FailureCode);
    }

    [Fact]
    public async Task Wallet_gateway_does_not_swallow_unknown_faults()
    {
        var actorId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var paymentId = Guid.Parse("01900000-0000-7000-8000-000000000902");
        var port = new RecordingWalletPort { SpendFaultCode = null };
        var actorCtx = new PaymentGatewayActorContext { ActorUserId = actorId };
        var gateway = new WalletPaymentGateway(port, actorCtx, new SystemUtcClock(), new ModuleCallTracer());
        var reference = WalletPaymentGateway.ComposeReference(paymentId, actorId, 50_000m, "IRR");

        await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.VerifyAsync(reference, true, CancellationToken.None));
    }

    [Fact]
    public void Contract_bridge_preserves_all_three_contract_gateways()
    {
        var type = typeof(Tooba.Payment.Infrastructure.Adapters.PaymentContractBridge);
        Assert.Contains(typeof(Tooba.Payment.Contracts.Admin.IPaymentAdminGateway), type.GetInterfaces());
        Assert.Contains(typeof(Tooba.Payment.Contracts.Customer.IPaymentCustomerGateway), type.GetInterfaces());
        Assert.Contains(typeof(Tooba.Payment.Contracts.Hold.IPaymentHoldSettingsGateway), type.GetInterfaces());
    }

    [Fact]
    public void Directory_ports_resolve_to_distinct_focused_implementations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOutboxPollTargetSource, NullTargetSource>();
        services.AddSingleton<IWorkerCommerceContextFactory, ThrowingCommerceContextFactory>();
        services.AddSingleton<IBackgroundWorkerRegistry, NullWorkerRegistry>();
        new Tooba.Payment.Infrastructure.DependencyInjection.PaymentModule()
            .AddServices(services, new ConfigurationBuilder().Build(), new EnvironmentStub());

        foreach (var (serviceType, implementationType) in new[]
        {
            (typeof(IPaymentDirectory), typeof(PaymentDirectory)),
            (typeof(IPaymentReconciliationDirectory), typeof(PaymentReconciliationDirectory)),
            (typeof(IPaymentAdminDirectory), typeof(PaymentAdminDirectory)),
            (typeof(IPaymentExpiryDirectory), typeof(PaymentExpiryDirectory)),
        })
        {
            var descriptors = services.Where(d => d.ServiceType == serviceType).ToArray();
            Assert.Single(descriptors);
            Assert.Equal(ServiceLifetime.Scoped, descriptors[0].Lifetime);
            Assert.NotSame(descriptors[0].ImplementationType, typeof(PaymentDirectory));
            Assert.NotEqual(serviceType, implementationType);
        }

        // Each focused implementation must implement exactly its own port among the four.
        var ports = new[] { typeof(IPaymentDirectory), typeof(IPaymentReconciliationDirectory), typeof(IPaymentAdminDirectory), typeof(IPaymentExpiryDirectory) };
        foreach (var (port, implementation) in new[]
        {
            (typeof(IPaymentDirectory), typeof(PaymentDirectory)),
            (typeof(IPaymentReconciliationDirectory), typeof(PaymentReconciliationDirectory)),
            (typeof(IPaymentAdminDirectory), typeof(PaymentAdminDirectory)),
            (typeof(IPaymentExpiryDirectory), typeof(PaymentExpiryDirectory)),
        })
        {
            var implemented = implementation.GetInterfaces().Where(ports.Contains).ToArray();
            Assert.Equal(new[] { port }, implemented);
        }

        var types = new[]
        {
            typeof(PaymentDirectory), typeof(PaymentReconciliationDirectory),
            typeof(PaymentAdminDirectory), typeof(PaymentExpiryDirectory),
        };
        Assert.Equal(4, types.Distinct().Count());
    }

    [Fact]
    public async Task Stale_reconciliation_delegates_to_canonical_verify()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.Parse("2026-09-24T12:00:00Z");
        var payment = SeedPendingPayment(db, now.AddHours(-1));
        var core = CreateDirectory(db, new ThrowingRefundGateway("payment.refund.gateway.declined"));
        var reconciliation = new PaymentReconciliationDirectory(db, core);

        var processed = await reconciliation.ReconcileStalePendingAsync(now, TimeSpan.FromMinutes(5), 10, CancellationToken.None);

        Assert.Equal(1, processed);
        var reloaded = await db.Payments.AsNoTracking().SingleAsync(x => x.PaymentId == payment.PaymentId);
        Assert.Equal(PaymentStatus.Failed, reloaded.Status);
    }

    [Fact]
    public async Task Admin_reconcile_uses_canonical_verify_path()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.Parse("2026-09-24T12:00:00Z");
        var payment = SeedPendingPayment(db, now);
        var core = CreateDirectory(db, new ThrowingRefundGateway("payment.refund.gateway.declined"));
        var admin = CreateAdminDirectory(db, core, new ThrowingRefundGateway("payment.refund.gateway.declined"));

        var result = await admin.ReconcileAsync(payment.PaymentId, CancellationToken.None);

        Assert.Equal(PaymentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Unpaid_reopen_expired_creates_new_attempt()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.Parse("2026-09-24T12:00:00Z");
        var payment = SeedPendingPayment(db, now, timeoutAt: now.AddMinutes(1));
        payment.ExpireUnpaidTimeout(now.AddMinutes(2));
        db.SaveChanges();

        var expiry = new PaymentExpiryDirectory(
            db,
            new OpenPaymentUseCaseGuard(),
            new PermissivePayableCheckoutReader(),
            new FakeGatewayRegistry(),
            new PaymentGatewayActorContext(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

        await expiry.ReopenExpiredForRetryAsync(payment.PaymentId, Guid.NewGuid(), null, CancellationToken.None);

        var reloaded = await db.Payments.AsNoTracking().SingleAsync(x => x.PaymentId == payment.PaymentId);
        Assert.Equal(PaymentStatus.Pending, reloaded.Status);
        Assert.Equal(2, await db.Attempts.CountAsync(x => x.PaymentId == payment.PaymentId));
    }

    [Fact]
    public async Task Admin_grid_missing_enrichment_returns_empty_display_strings()
    {
        var checkoutId = Guid.Parse("01900000-0000-7000-8000-000000000903");
        var handler = new Tooba.Payment.Application.Queries.QueryAdminPaymentsGrid.QueryAdminPaymentsGridHandler(
            new StubQueryDirectory(checkoutId),
            new StubEnrichmentReader());

        var result = await handler.Handle(
            new Tooba.Payment.Application.Queries.QueryAdminPaymentsGrid.QueryAdminPaymentsGridQuery(
                new AdminPaymentGridQueryInput(null, [], "created", "desc", 1, 20)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(string.Empty, item.CustomerDisplayName);
        Assert.Equal(string.Empty, item.ReservationLabel);
        Assert.Equal(string.Empty, item.ReservationLabelEn);
    }

    private static PaymentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase("payment-precert-" + Guid.NewGuid().ToString("N"))
            .Options);

    private static CustomerPayment SeedSucceededPayment(PaymentDbContext db)
    {
        var payment = SeedPendingPayment(db, DateTimeOffset.Parse("2026-09-24T12:00:00Z"));
        var attempt = payment.Attempts.Single();
        payment.ApplyVerifiedSuccess(attempt.AttemptId, "txn-precert", payment.CreatedAt.AddSeconds(2));
        db.SaveChanges();
        return payment;
    }

    private static CustomerPayment SeedPendingPayment(
        PaymentDbContext db,
        DateTimeOffset now,
        DateTimeOffset? timeoutAt = null)
    {
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1000m,
            "IRR",
            "fake",
            "idem-precert-" + Guid.NewGuid().ToString("N"),
            [(PaymentAllocationTargetKind.SellerOrder, Guid.NewGuid(), 1000m, Guid.NewGuid())],
            now);
        var attempt = payment.RecordInitiation(Guid.NewGuid(), "ref-precert", now.AddSeconds(1));
        if (timeoutAt is { } due)
        {
            payment.AssignUnpaidTimeout(due, now.AddSeconds(1));
        }
        else
        {
            payment.AssignUnpaidTimeout(now.AddHours(1), now.AddSeconds(1));
        }

        _ = attempt;
        db.Payments.Add(payment);
        db.Allocations.AddRange(payment.Allocations);
        var attempts = new List<PaymentAttempt>();
        foreach (var loaded in payment.Attempts)
        {
            attempts.Add(loaded);
        }

        db.Attempts.AddRange(attempts);
        db.SaveChanges();
        return payment;
    }

    private static PaymentDirectory CreateDirectory(PaymentDbContext db, IPaymentRefundGateway refundGateway) =>
        new(
            db,
            new OpenPaymentUseCaseGuard(),
            new PermissivePayableCheckoutReader(),
            new FailingGatewayRegistry(),
            new PaymentGatewayActorContext(),
            new SystemUtcClock(),
            new UuidV7IdGenerator(),
            () => CreateAdminDirectory(db, refundGateway));

    private static PaymentAdminDirectory CreateAdminDirectory(
        PaymentDbContext db,
        IPaymentRefundGateway refundGateway) =>
        CreateAdminDirectory(db, CreateDirectory(db, refundGateway), refundGateway);

    private static PaymentAdminDirectory CreateAdminDirectory(
        PaymentDbContext db,
        IPaymentDirectory core,
        IPaymentRefundGateway refundGateway) =>
        new(
            db,
            new OpenPaymentUseCaseGuard(),
            core,
            new SystemUtcClock(),
            new UuidV7IdGenerator(),
            refundGateway);

    private sealed class ThrowingRefundGateway(string? code) : IPaymentRefundGateway
    {
        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            _ = paymentId;
            _ = amount;
            _ = currency;
            _ = idempotencyKey;
            return code is null
                ? throw new InvalidOperationException("unknown.refund.fault")
                : Task.FromException<GatewayRefundResult>(new ContractOperationException(code));
        }
    }

    private sealed class UnusedPayableCheckoutReader : IPayableCheckoutReader
    {
        public Task<PayableCheckoutSnapshot?> GetPayableAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PayableCheckoutSnapshot?>(null);
    }

    /// <summary>مالکیت پرداخت برای مسیر Verify/Reconciliation را می‌پذیرد (بدون قرارداد سفارش).</summary>
    private sealed class PermissivePayableCheckoutReader : IPayableCheckoutReader
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

    private sealed class UnusedGatewayRegistry : IPaymentGatewayRegistry
    {
        public IPaymentGateway Resolve(string providerCode) =>
            throw new ContractOperationException("payment.gateway.unavailable");
    }

    /// <summary>درگاه fake برای Verify؛ شکست قطعی می‌دهد تا مسیر Failed قابل اثبات باشد.</summary>
    private sealed class FailingGatewayRegistry : IPaymentGatewayRegistry
    {
        private readonly IPaymentGateway _gateway = new FakeFailingPaymentGateway(new SystemUtcClock());

        public IPaymentGateway Resolve(string providerCode) => _gateway;
    }

    private sealed class FakeGatewayRegistry : IPaymentGatewayRegistry
    {
        private readonly FakePaymentGateway _gateway = new(new SystemUtcClock(), new UuidV7IdGenerator());

        public IPaymentGateway Resolve(string providerCode) => _gateway;
    }

    private sealed class RecordingWalletPort : IWalletOrderPaymentPort
    {
        public string? SpendFaultCode { get; init; } = "wallet.rejected.2YXZiNis";

        public Task<WalletOrderPaymentDebitResultDto> SpendForOrderPaymentAsync(
            Guid customerActorId,
            decimal amount,
            string currency,
            Guid paymentId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            _ = customerActorId;
            _ = amount;
            _ = currency;
            _ = paymentId;
            _ = idempotencyKey;
            return SpendFaultCode is null
                ? Task.FromException<WalletOrderPaymentDebitResultDto>(new InvalidOperationException("wallet.unexpected.fault"))
                : Task.FromException<WalletOrderPaymentDebitResultDto>(new ContractOperationException(SpendFaultCode));
        }

        public Task<WalletCheckoutQuoteDto> QuoteForPayableAsync(
            Guid customerActorId,
            decimal payableAmount,
            string currency,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletCheckoutQuoteDto(payableAmount, payableAmount, 0m, true, currency));
    }

    private sealed class StubQueryDirectory(Guid checkoutId) : IPaymentQueryDirectory
    {
        public Task<IReadOnlyList<PaymentCheckoutRowDto>> GetLatestByCheckoutIdsAsync(
            IReadOnlyCollection<Guid> checkoutIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PaymentCheckoutRowDto>>([]);

        public Task<PaymentAdminGridPageDto> QueryAdminGridAsync(
            PaymentAdminGridQueryDto query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentAdminGridPageDto(
                [new PaymentCheckoutRowDto(
                    Guid.NewGuid(), checkoutId, 1000m, "IRR", "Succeeded", "manual",
                    DateTimeOffset.Parse("2026-09-24T12:00:00Z"), null, null)],
                1));
    }

    private sealed class StubEnrichmentReader : Tooba.Order.Contracts.Payments.IPaymentAdminOrderEnrichmentReader
    {
        public Task<IReadOnlyList<Guid>> ResolveSearchCheckoutIdsAsync(string search, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> ResolveSupplyFilterCheckoutIdsAsync(
            IReadOnlyList<string> wantedValues,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> ResolveReservationFilterCheckoutIdsAsync(
            IReadOnlyList<string> wantedValues,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Tooba.Order.Contracts.Payments.AdminPaymentOrderEnrichmentSnapshot>> EnrichAsync(
            IReadOnlyList<Guid> checkoutIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tooba.Order.Contracts.Payments.AdminPaymentOrderEnrichmentSnapshot>>([]);
    }

    private sealed class NullTargetSource : IOutboxPollTargetSource
    {
        public IReadOnlyList<OutboxPollTarget> GetTargets() => [];
    }

    private sealed class ThrowingCommerceContextFactory : IWorkerCommerceContextFactory
    {
        public CommerceContext FromPollTarget(OutboxPollTarget target, string traceId) =>
            throw new InvalidOperationException("commerce.context.not_expected_in_test");
    }

    private sealed class NullWorkerRegistry : IBackgroundWorkerRegistry
    {
        public void RecordSuccess(string workerName, int processedCount)
        {
        }

        public void RecordFailure(string workerName, string errorType)
        {
        }
    }

    private sealed class EnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tooba.Payment.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

