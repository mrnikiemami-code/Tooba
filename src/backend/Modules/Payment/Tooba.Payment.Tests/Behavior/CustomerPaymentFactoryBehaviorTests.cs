using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

public sealed class CustomerPaymentFactoryBehaviorTests
{
    [Fact]
    public void Open_requires_explicit_payment_and_allocation_ids_and_stays_pending()
    {
        var now = DateTimeOffset.Parse("2026-03-21T12:00:00Z");
        var paymentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var checkoutId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var allocationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var sellerOrderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var payment = CustomerPayment.Open(
            paymentId,
            checkoutId,
            1000m,
            "IRR",
            "manual",
            "idem-behavior-1",
            [(PaymentAllocationTargetKind.SellerOrder, sellerOrderId, 1000m, allocationId)],
            now);

        Assert.Equal(paymentId, payment.PaymentId);
        Assert.Equal(checkoutId, payment.CheckoutId);
        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Single(payment.Allocations);
        Assert.Equal(allocationId, payment.Allocations.Single().AllocationId);
    }

    [Fact]
    public void RecordInitiation_uses_caller_supplied_attempt_id()
    {
        var now = DateTimeOffset.Parse("2026-03-21T12:00:00Z");
        var payment = CustomerPayment.Open(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            500m,
            "IRR",
            "manual",
            "idem-behavior-2",
            [(PaymentAllocationTargetKind.SellerOrder, Guid.Parse("44444444-4444-4444-4444-444444444444"), 500m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            now);
        var attemptId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var attempt = payment.RecordInitiation(attemptId, "manual-ref", now.AddSeconds(1));
        Assert.Equal(attemptId, attempt.AttemptId);
        Assert.Equal(PaymentAttemptStatus.Initiated, attempt.Status);
    }
}
