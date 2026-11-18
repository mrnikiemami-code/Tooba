namespace Tooba.Story.Domain.Aggregates;

/// <summary>Durable Story command occurrence for exact replay of create/lifecycle intents.</summary>
public sealed class StoryCommandOccurrence
{
    /// <summary>Stable occurrence key supplied by the client.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Optional seller scope.</summary>
    public Guid? SellerPartyId { get; init; }

    /// <summary>Command family token (create, submit, approve_publish, ...).</summary>
    public string ActionCode { get; init; } = string.Empty;

    /// <summary>SHA-256 of the canonical immutable intent body.</summary>
    public string IntentHash { get; init; } = string.Empty;

    /// <summary>Story id produced or targeted by the occurrence.</summary>
    public Guid StoryId { get; init; }

    /// <summary>Actor that first recorded the occurrence.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>Recording instant.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
