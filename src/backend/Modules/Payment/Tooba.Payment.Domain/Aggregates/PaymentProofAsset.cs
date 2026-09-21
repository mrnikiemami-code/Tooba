using Tooba.Payment.Domain.ValueObjects;

namespace Tooba.Payment.Domain.Aggregates;

/// <summary>
/// دارایی مدرک واریز متصل به پرداخت؛ فقط MediaAssetId نگه داشته می‌شود.
/// </summary>
public sealed class PaymentProofAsset
{
    /// <summary>سازندهٔ EF.</summary>
    private PaymentProofAsset()
    {
    }

    /// <summary>شناسهٔ ردیف اتصال.</summary>
    public Guid ProofAssetRowId { get; init; }

    /// <summary>پرداخت مالک.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>دارایی Media آپلودشده برای همین پرداخت.</summary>
    public Guid MediaAssetId { get; init; }

    /// <summary>زمان اتصال.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>اتصال مدرک را می‌سازد.</summary>
    public static PaymentProofAsset Attach(Guid proofAssetRowId, Guid paymentId, Guid mediaAssetId, DateTimeOffset at)
    {
        if (proofAssetRowId == Guid.Empty || paymentId == Guid.Empty || mediaAssetId == Guid.Empty)
        {
            throw new InvalidOperationException("payment.proof.invalid");
        }

        return new PaymentProofAsset
        {
            ProofAssetRowId = proofAssetRowId,
            PaymentId = paymentId,
            MediaAssetId = mediaAssetId,
            CreatedAt = at,
        };
    }
}
