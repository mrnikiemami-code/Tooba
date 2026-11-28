namespace Tooba.Pricing.Domain.Aggregates;

/// <summary>Owner-local durable fact written in the same Pricing transaction as the price mutation.</summary>
public sealed class PriceChangeFact
{
    /// <summary>Stable fact identity.</summary>
    public Guid FactId { get; init; }

    /// <summary>Offer identity.</summary>
    public Guid OfferId { get; init; }

    /// <summary>Price identity.</summary>
    public Guid PriceId { get; init; }

    /// <summary>Idempotent caller operation identity.</summary>
    public Guid OperationId { get; init; }

    /// <summary>Authenticated actor when available.</summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>Owner commit instant.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Previous committed amount, or null on first creation.</summary>
    public decimal? OldAmount { get; init; }

    /// <summary>New committed amount.</summary>
    public decimal NewAmount { get; init; }

    /// <summary>True for retirement: the retained amount is historical, not a new effective price.</summary>
    public bool Retired { get; init; }

    /// <summary>Price currency.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>Owner CAS revision for this committed fact.</summary>
    public string Revision { get; init; } = string.Empty;
}
