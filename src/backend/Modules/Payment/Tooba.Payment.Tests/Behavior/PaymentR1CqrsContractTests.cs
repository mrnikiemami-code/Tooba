using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Admin.Commands;
using Tooba.Payment.Application.Webhooks.Commands;
using Tooba.Payment.Application.Reconciliation.Commands;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Contracts.Errors;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Application.Admin.Queries;
using Tooba.Payment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

public sealed class PaymentR1CqrsContractTests
{
    [Fact]
    public void ProcessPaymentWebhook_rejects_invalid_signature()
    {
        var handler = new ProcessPaymentWebhookHandler(
            new StubSignatures(false, PaymentErrorCodes.WebhookInvalidSignature),
            new StubWebhookHandler());
        var result = handler.Handle(
            new ProcessPaymentWebhookCommand("fake", [1], "bad", "{}"),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrorCodes.WebhookInvalidSignature, result.Errors[0].Code);
    }

    [Fact]
    public void ProcessPaymentWebhook_rejects_invalid_payload()
    {
        var handler = new ProcessPaymentWebhookHandler(
            new StubSignatures(true, string.Empty),
            new StubWebhookHandler());
        var result = handler.Handle(
            new ProcessPaymentWebhookCommand("fake", [1], "sha256=aa", "{"),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrorCodes.WebhookInvalidPayload, result.Errors[0].Code);
    }

    [Fact]
    public void GetAdminPayment_missing_uses_admin_code()
    {
        var handler = new GetAdminPaymentHandler(new StubAdminDirectory());
        var result = handler.Handle(new GetAdminPaymentQuery(Guid.NewGuid()), CancellationToken.None)
            .GetAwaiter().GetResult();
        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrorCodes.AdminPaymentMissing, result.Errors[0].Code);
    }

    [Fact]
    public void Admin_actions_map_stable_codes_only()
    {
        var payments = new StubAdminDirectory { ThrowCode = PaymentErrorCodes.MethodNotManual };
        Assert.Equal(
            PaymentErrorCodes.MethodNotManual,
            new ConfirmAdminDepositHandler(payments)
                .Handle(new ConfirmAdminDepositCommand(Guid.NewGuid()), CancellationToken.None)
                .GetAwaiter().GetResult().Errors[0].Code);
        Assert.Equal(
            PaymentErrorCodes.MethodNotManual,
            new RejectAdminDepositHandler(payments)
                .Handle(new RejectAdminDepositCommand(Guid.NewGuid()), CancellationToken.None)
                .GetAwaiter().GetResult().Errors[0].Code);
        payments.ThrowCode = PaymentErrorCodes.Missing;
        Assert.Equal(
            PaymentErrorCodes.Missing,
            new ReconcileAdminPaymentHandler(payments)
                .Handle(new ReconcileAdminPaymentCommand(Guid.NewGuid()), CancellationToken.None)
                .GetAwaiter().GetResult().Errors[0].Code);
    }

    [Fact]
    public void ReconcileStalePayments_uses_clock_not_utc_now()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var directory = new StubReconciliation();
        var handler = new ReconcileStalePaymentsHandler(directory, clock);
        var result = handler.Handle(
            new ReconcileStalePaymentsCommand(TimeSpan.FromMinutes(5), 10),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
        Assert.Equal(clock.UtcNow, directory.SeenNow);
    }

    [Fact]
    public void Declared_codes_are_known_and_foreign_codes_are_not()
    {
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.Missing));
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.UnpaidSupplyUnavailable));
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.SupplyUnavailable));
        Assert.False(PaymentErrorCodes.IsKnown("payment missing somewhere"));
        Assert.False(PaymentErrorCodes.IsKnown("پرداخت پیدا نشد"));
        Assert.False(PaymentErrorCodes.IsKnown(null));
        Assert.False(PaymentErrorCodes.IsKnown(" "));
    }

    [Fact]
    public void Operation_seam_maps_declared_codes_and_rethrows_foreign_codes()
    {
        var mapped = PaymentOperation
            .ExecuteAsync(() => Task.FromException<string>(new ContractOperationException(PaymentErrorCodes.Missing)))
            .GetAwaiter().GetResult();
        Assert.True(mapped.IsFailure);
        Assert.Equal(PaymentErrorCodes.Missing, mapped.Errors[0].Code);

        var foreign = Assert.ThrowsAsync<ContractOperationException>(() => PaymentOperation
            .ExecuteAsync(() => Task.FromException<string>(new ContractOperationException("order.some_foreign_code"))));
        Assert.Equal("order.some_foreign_code", foreign.Result.Code);

        var unknown = Assert.ThrowsAsync<InvalidOperationException>(() => PaymentOperation
            .ExecuteAsync(() => Task.FromException<string>(new InvalidOperationException("unexpected"))));
        Assert.Equal("unexpected", unknown.Result.Message);
    }

    private sealed class StubSignatures(bool ok, string error) : IPaymentWebhookSignatureVerifier
    {
        public string SignatureHeaderName => "X-Tooba-Payment-Signature";
        public bool TryValidate(ReadOnlySpan<byte> body, string? signatureHeader, out string errorCode)
        {
            errorCode = error;
            return ok;
        }
    }

    private sealed class StubWebhookHandler : IPaymentWebhookHandler
    {
        public Task<PaymentWebhookHandleResult> HandleAsync(
            string providerCode, PaymentWebhookNotification notification, CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentWebhookHandleResult(true, false, null));
    }

    private sealed class StubAdminDirectory : IPaymentAdminDirectory
    {
        public string? ThrowCode { get; set; }

        public Task<PaymentOperationalSnapshot?> GetOperationalAsync(Guid paymentId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentOperationalSnapshot?>(null);

        public Task<PaymentOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
            Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentOperationalSnapshot?>(null);

        public Task<PaymentVerificationResult> ReconcileAsync(Guid paymentId, CancellationToken cancellationToken) =>
            ThrowOrDefault(paymentId);

        public Task<PaymentVerificationResult> ConfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            ThrowOrDefault(paymentId);

        public Task<PaymentVerificationResult> RejectDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            ThrowOrDefault(paymentId);

        public Task<PaymentVerificationResult> RestoreDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            ThrowOrDefault(paymentId);

        public Task<PaymentVerificationResult> UnconfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            ThrowOrDefault(paymentId);

        public Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RestoreAfterOrderCancelRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        private Task<PaymentVerificationResult> ThrowOrDefault(Guid paymentId)
        {
            if (ThrowCode is not null)
                throw new ContractOperationException(ThrowCode);
            return Task.FromResult(new PaymentVerificationResult(paymentId, PaymentStatus.Pending, false));
        }
    }

    private sealed class StubReconciliation : IPaymentReconciliationDirectory
    {
        public DateTimeOffset SeenNow { get; private set; }

        public Task<int> ReconcileStalePendingAsync(
            DateTimeOffset asOf, TimeSpan minAge, int batchSize, CancellationToken cancellationToken)
        {
            SeenNow = asOf;
            return Task.FromResult(3);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
