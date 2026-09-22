using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Commands.ConfirmAdminDeposit;
using Tooba.Payment.Application.Commands.ProcessPaymentWebhook;
using Tooba.Payment.Application.Commands.ReconcileAdminPayment;
using Tooba.Payment.Application.Commands.ReconcileStalePayments;
using Tooba.Payment.Application.Commands.RejectAdminDeposit;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Application.Queries.GetAdminPayment;
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
    public void Exception_mapper_exact_codes_only()
    {
        Assert.True(PaymentExceptionMapper.TryMapExact(PaymentErrorCodes.Missing, out var mapped));
        Assert.Equal(PaymentErrorCodes.Missing, mapped.Code);
        Assert.False(PaymentExceptionMapper.TryMapExact("payment missing somewhere", out _));
        Assert.False(PaymentExceptionMapper.TryMapExact("پرداخت پیدا نشد", out _));
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
                throw new InvalidOperationException(ThrowCode);
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
