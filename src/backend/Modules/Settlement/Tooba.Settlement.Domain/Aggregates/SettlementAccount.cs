using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.Aggregates;

/// <summary>
/// Domain type.
/// </summary>
public sealed class SettlementAccount
{
    private SettlementAccount()
    {
    }

    /// <summary>شناسه حساب.</summary>
    public Guid SettlementAccountId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>ارز حساب.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>حساب جدید برای فروشنده می‌سازد.</summary>
    public static SettlementAccount Create(Guid id, Guid sellerPartyId, string currency, DateTimeOffset now) =>
        new()
        {
            SettlementAccountId = id,
            SellerPartyId = sellerPartyId,
            Currency = currency.Trim(),
            CreatedAt = now,
        };
}
