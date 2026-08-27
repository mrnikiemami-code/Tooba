namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// تله‌متری سبک Settlement.
/// </summary>
public sealed class SettlementInstrumentation
{
    /// <summary>posted entry ثبت می‌کند.</summary>
    public void RecordEntryPosted() { }

    /// <summary>payout موفق ثبت می‌کند.</summary>
    public void RecordPayoutSucceeded() { }

    /// <summary>payout شکست‌خورده ثبت می‌کند.</summary>
    public void RecordPayoutFailed() { }
}
