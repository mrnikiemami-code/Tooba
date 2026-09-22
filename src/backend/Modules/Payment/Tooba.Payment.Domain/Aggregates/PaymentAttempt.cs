using Tooba.Payment.Domain.ValueObjects;

using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Aggregates;

/// <summary>
/// یک تلاش درگاه. شناسهٔ داخلی با شمارهٔ تراکنش ارائه‌دهنده یکی نیست.
/// </summary>
public sealed class PaymentAttempt
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private PaymentAttempt()
    {
    }

    /// <summary>
    /// شناسهٔ تلاش.
    /// </summary>
    public Guid AttemptId { get; init; }

    /// <summary>
    /// پرداخت مالک.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// کد درگاه انتزاعی.
    /// </summary>
    public string ProviderCode { get; init; } = string.Empty;

    /// <summary>
    /// مرجع درخواست نزد درگاه.
    /// </summary>
    public string ProviderRequestReference { get; private set; } = string.Empty;

    /// <summary>
    /// مرجع تراکنش پس از تأیید؛ کلید یکتایی درگاه است نه PK داخلی.
    /// </summary>
    public string? ProviderTransactionReference { get; private set; }

    /// <summary>
    /// وضعیت تلاش.
    /// </summary>
    public PaymentAttemptStatus Status { get; private set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان پایان تلاش.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// کد شکست درگاه در صورت وجود.
    /// </summary>
    public string? FailureCode { get; private set; }

    /// <summary>
    /// شماره پیگیری واریز مشتری (کارت‌به‌کارت). شماره پیگیری سفارش نیست.
    /// </summary>
    public string? CustomerTransferReference { get; private set; }

    /// <summary>
    /// شناسهٔ دارایی رسانه برای مدرک واریز؛ مسیر فایل خام ذخیره نمی‌شود.
    /// </summary>
    public Guid? ProofMediaAssetId { get; private set; }

    /// <summary>
    /// زمان ثبت مدرک/پیگیری توسط مشتری.
    /// </summary>
    public DateTimeOffset? EvidenceSubmittedAt { get; private set; }

    /// <summary>
    /// سقف طول شماره پیگیری پرداخت.
    /// </summary>
    public const int CustomerTransferReferenceMaxLength = 64;

    /// <summary>
    /// تلاش را پس از شروع درگاه می‌سازد.
    /// </summary>
    public static PaymentAttempt Initiate(Guid attemptId, Guid paymentId, string providerCode, string requestReference, DateTimeOffset at)
    {
        if (attemptId == Guid.Empty || paymentId == Guid.Empty)
            throw new ContractOperationException("payment.attempt.ids_required");
        return new PaymentAttempt
        {
            AttemptId = attemptId,
            PaymentId = paymentId,
            ProviderCode = providerCode,
            ProviderRequestReference = requestReference,
            Status = PaymentAttemptStatus.Initiated,
            CreatedAt = at,
        };
    }

    /// <summary>
    /// تأیید موفق درگاه را روی همین تلاش ثبت می‌کند؛ تلاش قبلی را پاک نمی‌کند.
    /// </summary>
    public void MarkVerifiedSuccess(string transactionReference, DateTimeOffset at)
    {
        if (Status != PaymentAttemptStatus.Initiated)
        {
            return;
        }

        ProviderTransactionReference = transactionReference;
        Status = PaymentAttemptStatus.VerifiedSucceeded;
        CompletedAt = at;
    }

    /// <summary>
    /// تأیید ناموفق درگاه را ثبت می‌کند.
    /// </summary>
    public void MarkVerifiedFailure(string? failureCode, DateTimeOffset at)
    {
        if (Status != PaymentAttemptStatus.Initiated)
        {
            return;
        }

        FailureCode = failureCode;
        Status = PaymentAttemptStatus.VerifiedFailed;
        CompletedAt = at;
    }

    /// <summary>
    /// مدرک کارت‌به‌کارت را روی تلاش Initiated ثبت می‌کند؛ تلاش تاریخی را بازنویسی نمی‌کند.
    /// </summary>
    public void SubmitCustomerEvidence(string transferReference, Guid? proofMediaAssetId, DateTimeOffset at)
    {
        var trimmed = (transferReference ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ContractOperationException("payment.tracking_reference.required");
        }

        if (trimmed.Length > CustomerTransferReferenceMaxLength)
        {
            throw new ContractOperationException("payment.tracking_reference.too_long");
        }

        if (Status != PaymentAttemptStatus.Initiated)
        {
            throw new ContractOperationException("payment.manual.evidence.immutable");
        }

        if (EvidenceSubmittedAt is not null)
        {
            var sameRef = string.Equals(CustomerTransferReference, trimmed, StringComparison.Ordinal);
            var sameProof = ProofMediaAssetId == proofMediaAssetId;
            if (sameRef && sameProof)
            {
                return;
            }

            throw new ContractOperationException("payment.manual.evidence.duplicate");
        }

        CustomerTransferReference = trimmed;
        ProofMediaAssetId = proofMediaAssetId;
        EvidenceSubmittedAt = at;
    }
}
