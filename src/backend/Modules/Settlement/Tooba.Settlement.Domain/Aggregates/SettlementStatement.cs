using Tooba.BuildingBlocks;
using Tooba.Settlement.Domain.ValueObjects;

namespace Tooba.Settlement.Domain.Aggregates;

/// <summary>
/// Domain type.
/// </summary>
public sealed class SettlementStatement
{
    private SettlementStatement()
    {
    }

    /// <summary>شناسه صورت‌حساب.</summary>
    public Guid StatementId { get; init; }

    /// <summary>حساب مالک.</summary>
    public Guid SettlementAccountId { get; init; }

    /// <summary>وضعیت دوره.</summary>
    public StatementStatus Status { get; private set; }

    /// <summary>شروع دوره.</summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>پایان دوره.</summary>
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>مانده ابتدای دوره.</summary>
    public decimal OpeningBalance { get; init; }

    /// <summary>مانده انتهای دوره.</summary>
    public decimal ClosingBalance { get; private set; }

    /// <summary>ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>صورت‌حساب باز می‌سازد.</summary>
    public static SettlementStatement Open(
        Guid id,
        Guid settlementAccountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        decimal openingBalance,
        string currency,
        DateTimeOffset now) =>
        new()
        {
            StatementId = id,
            SettlementAccountId = settlementAccountId,
            Status = StatementStatus.Open,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            OpeningBalance = openingBalance,
            ClosingBalance = openingBalance,
            Currency = currency.Trim(),
            CreatedAt = now,
        };

    /// <summary>دوره را می‌بندد.</summary>
    public void Close(decimal closingBalance, DateTimeOffset now)
    {
        _ = now;
        Status = StatementStatus.Closed;
        ClosingBalance = closingBalance;
    }
}
