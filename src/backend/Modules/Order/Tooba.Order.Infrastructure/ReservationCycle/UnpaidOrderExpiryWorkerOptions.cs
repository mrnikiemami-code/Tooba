namespace Tooba.Order.Infrastructure.ReservationCycle;

/// <summary>Configuration for the Order-owned unpaid-order expiry worker.</summary>
internal sealed class UnpaidOrderExpiryWorkerOptions
{
    public const string SectionName = "Tooba:UnpaidOrderExpiry";

    /// <summary>If false, the worker exits without polling.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Polling interval in seconds.</summary>
    public int PollIntervalSeconds { get; set; } = 15;

    /// <summary>Maximum unpaid payments processed per claim.</summary>
    public int BatchSize { get; set; } = 20;
}
