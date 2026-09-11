using Tooba.Payment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T004-R2 — مدرک کارت‌به‌کارت و تلاش مجدد بدون بازنویسی تاریخچه.
/// </summary>
public sealed class StorefrontPaymentCompletionDomainTests
{
    [Fact]
    public void Manual_evidence_requires_trimmed_tracking_and_stays_pending()
    {
        var payment = OpenManual();
        var attempt = payment.RecordInitiation("manual-ref", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() =>
            payment.SubmitManualEvidence("   ", null, DateTimeOffset.UtcNow));
        var missing = Assert.Throws<InvalidOperationException>(() =>
            payment.SubmitManualEvidence("   ", null, DateTimeOffset.UtcNow));
        Assert.Equal("شماره پیگیری پرداخت الزامی است.", missing.Message);

        payment.SubmitManualEvidence("  ABC123  ", null, DateTimeOffset.UtcNow);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("ABC123", attempt.CustomerTransferReference);
        payment.SubmitManualEvidence("ABC123", null, DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void Rejected_manual_retry_creates_new_attempt_and_keeps_old_evidence()
    {
        var payment = OpenManual();
        var first = payment.RecordInitiation("manual-ref", DateTimeOffset.UtcNow);
        payment.SubmitManualEvidence("TRK-1", Guid.NewGuid(), DateTimeOffset.UtcNow);
        payment.ApplyVerifiedFailure(first.AttemptId, "MANUAL_DEPOSIT_REJECTED", DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("TRK-1", first.CustomerTransferReference);
        Assert.Equal(PaymentAttemptStatus.VerifiedFailed, first.Status);

        var retry = payment.RestoreRejectedManualToPending(DateTimeOffset.UtcNow.AddSeconds(2));
        Assert.NotEqual(first.AttemptId, retry.AttemptId);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("TRK-1", first.CustomerTransferReference);
        Assert.Null(retry.CustomerTransferReference);
    }

    private static CustomerPayment OpenManual()
    {
        var seller = Guid.NewGuid();
        return CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "manual",
            Guid.NewGuid().ToString("N"),
            new[] { (PaymentAllocationTargetKind.SellerOrder, seller, 1000m) },
            DateTimeOffset.UtcNow);
    }
}
