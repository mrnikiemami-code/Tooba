using Tooba.Host.Grid;
using Tooba.Order.Domain;
using Tooba.Returns.Domain;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class ManualPaymentAndListStatusTests
{
    [Fact]
    public void Manual_gateway_pending_until_confirm()
    {
        var gateway = new Tooba.Payment.Infrastructure.ManualPaymentGateway();
        var reference = $"manual-{Guid.NewGuid():N}";
        Tooba.Payment.Infrastructure.ManualPaymentGateway.ConfirmedReferences.TryRemove(reference, out _);
        Tooba.Payment.Infrastructure.ManualPaymentGateway.RejectedReferences.TryRemove(reference, out _);

        var pending = gateway.VerifyAsync(reference, true, CancellationToken.None).GetAwaiter().GetResult();
        Assert.False(pending.VerifiedSuccess);
        Assert.Equal("MANUAL_DEPOSIT_PENDING", pending.FailureCode);

        Tooba.Payment.Infrastructure.ManualPaymentGateway.Confirm(reference);
        var ok = gateway.VerifyAsync(reference, false, CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(ok.VerifiedSuccess);
        Assert.StartsWith("manual-txn-", ok.ProviderTransactionReference);
    }

    [Fact]
    public void Manual_gateway_reject_fails_verify()
    {
        var gateway = new Tooba.Payment.Infrastructure.ManualPaymentGateway();
        var reference = $"manual-{Guid.NewGuid():N}";
        Tooba.Payment.Infrastructure.ManualPaymentGateway.Reject(reference);
        var failed = gateway.VerifyAsync(reference, true, CancellationToken.None).GetAwaiter().GetResult();
        Assert.False(failed.VerifiedSuccess);
        Assert.Equal("MANUAL_DEPOSIT_REJECTED", failed.FailureCode);
    }

    [Fact]
    public void Compose_operational_status_prefers_return_refund_states()
    {
        var orderStatuses = new[] { SellerOrderStatus.Paid };
        Assert.Equal(
            "ReturnRequested",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.Requested]));
        Assert.Equal(
            "ReturnApproved",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.Approved]));
        Assert.Equal(
            "RefundPending",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.RefundProcessing]));
        Assert.Equal(
            "RefundCompleted",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.Completed]));
        Assert.Equal(
            "Paid",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, Array.Empty<ReturnRequestStatus>()));
    }
}
