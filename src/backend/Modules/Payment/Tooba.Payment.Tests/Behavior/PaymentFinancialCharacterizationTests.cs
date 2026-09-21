using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Providers;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// Focused financial characterization for Payment→Wallet gateway + lifecycle.
/// </summary>
public sealed class PaymentFinancialCharacterizationTests
{
    [Fact]
    public void Wallet_gateway_reference_preserves_amount_currency_customer_payment()
    {
        var paymentId = Guid.Parse("01900000-0000-7000-8000-000000000601");
        var actorId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var amount = 123_456m;
        var currency = "irr";

        var reference = WalletPaymentGateway.ComposeReference(paymentId, actorId, amount, WalletCurrency.Normalize(currency));
        Assert.True(WalletPaymentGateway.TryParseReference(reference, out var p, out var a, out var amt, out var cur));
        Assert.Equal(paymentId, p);
        Assert.Equal(actorId, a);
        Assert.Equal(123_456m, amt);
        Assert.Equal("IRR", cur);
        Assert.Contains(paymentId.ToString("N"), reference, StringComparison.Ordinal);
        Assert.Contains(actorId.ToString("N"), reference, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Wallet_gateway_verify_uses_wallet_order_debit_idempotency_key_pattern()
    {
        var paymentId = Guid.Parse("01900000-0000-7000-8000-000000000701");
        var actorId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var amount = 50_000m;
        var currency = "IRR";
        var port = new RecordingWalletPort();
        var actorCtx = new PaymentGatewayActorContext();
        actorCtx.ActorUserId = actorId;
        var gateway = new WalletPaymentGateway(port, actorCtx, new SystemUtcClock(), new ModuleCallTracer());
        var reference = WalletPaymentGateway.ComposeReference(paymentId, actorId, amount, currency);

        var result = await gateway.VerifyAsync(reference, true, CancellationToken.None);

        Assert.True(result.VerifiedSuccess);
        Assert.Equal($"wallet:{paymentId:D}", result.ProviderTransactionReference);
        Assert.Equal(paymentId, port.LastPaymentId);
        Assert.Equal(actorId, port.LastCustomerActorId);
        Assert.Equal(amount, port.LastAmount);
        Assert.Equal(currency, port.LastCurrency);
        Assert.Equal($"wallet-order-debit:{paymentId:D}", port.LastIdempotencyKey);
    }

    [Fact]
    public void Payment_lifecycle_success_and_failure_transitions()
    {
        var now = DateTimeOffset.Parse("2026-03-21T12:00:00Z");
        var paymentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var checkoutId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var allocationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var sellerOrderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var attemptId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var success = CustomerPayment.Open(
            paymentId,
            checkoutId,
            1000m,
            "IRR",
            "manual",
            "idem-lifecycle-ok",
            [(PaymentAllocationTargetKind.SellerOrder, sellerOrderId, 1000m, allocationId)],
            now);
        success.RecordInitiation(attemptId, "ref-ok", now.AddSeconds(1));
        success.ApplyVerifiedSuccess(attemptId, "txn-ok", now.AddSeconds(2));
        Assert.Equal(PaymentStatus.Succeeded, success.Status);

        var failure = CustomerPayment.Open(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            checkoutId,
            1000m,
            "IRR",
            "manual",
            "idem-lifecycle-fail",
            [(PaymentAllocationTargetKind.SellerOrder, sellerOrderId, 1000m, Guid.Parse("77777777-7777-7777-7777-777777777777"))],
            now);
        var failAttempt = Guid.Parse("88888888-8888-8888-8888-888888888888");
        failure.RecordInitiation(failAttempt, "ref-fail", now.AddSeconds(1));
        failure.ApplyVerifiedFailure(failAttempt, "DECLINED", now.AddSeconds(2));
        Assert.Equal(PaymentStatus.Failed, failure.Status);
    }

    [Fact]
    public void Webhook_inbox_record_dedup_key_is_provider_plus_event_id()
    {
        var now = DateTimeOffset.Parse("2026-03-21T12:00:00Z");
        var a = Tooba.Payment.Infrastructure.Persistence.PaymentWebhookInboxRecord.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "webhook",
            "evt-1",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            now);
        var b = Tooba.Payment.Infrastructure.Persistence.PaymentWebhookInboxRecord.Create(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "webhook",
            "evt-1",
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            now);
        Assert.Equal(a.ProviderCode, b.ProviderCode);
        Assert.Equal(a.ProviderEventId, b.ProviderEventId);
        Assert.NotEqual(a.InboxId, b.InboxId);
    }

    private sealed class RecordingWalletPort : IWalletOrderPaymentPort
    {
        public Guid LastCustomerActorId { get; private set; }
        public decimal LastAmount { get; private set; }
        public string? LastCurrency { get; private set; }
        public Guid LastPaymentId { get; private set; }
        public string? LastIdempotencyKey { get; private set; }

        public Task<WalletOrderPaymentDebitResultDto> SpendForOrderPaymentAsync(
            Guid customerActorId,
            decimal amount,
            string currency,
            Guid paymentId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastCustomerActorId = customerActorId;
            LastAmount = amount;
            LastCurrency = currency;
            LastPaymentId = paymentId;
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(new WalletOrderPaymentDebitResultDto(0m, false));
        }

        public Task<WalletCheckoutQuoteDto> QuoteForPayableAsync(
            Guid customerActorId,
            decimal payableAmount,
            string currency,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletCheckoutQuoteDto(payableAmount, payableAmount, 0m, true, currency));
    }
}
