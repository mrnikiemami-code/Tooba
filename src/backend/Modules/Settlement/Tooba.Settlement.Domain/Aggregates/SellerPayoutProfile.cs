using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.Aggregates;

/// <summary>
/// Domain type.
/// </summary>
public sealed class SellerPayoutProfile
{
    private SellerPayoutProfile()
    {
    }

    /// <summary>شناسه پروفایل.</summary>
    public Guid SellerPayoutProfileId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>شماره شبا/IBAN.</summary>
    public string? Iban { get; init; }

    /// <summary>نام صاحب حساب.</summary>
    public string? AccountHolderName { get; init; }

    /// <summary>آیا پروفایل تأیید شده.</summary>
    public bool IsVerified { get; init; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>پروفایل placeholder برای dev می‌سازد.</summary>
    public static SellerPayoutProfile CreateDevPlaceholder(Guid id, Guid sellerPartyId, DateTimeOffset now) =>
        new()
        {
            SellerPayoutProfileId = id,
            SellerPartyId = sellerPartyId,
            Iban = "IR000000000000000000000000",
            AccountHolderName = "dev-seller",
            IsVerified = true,
            CreatedAt = now,
        };
}
