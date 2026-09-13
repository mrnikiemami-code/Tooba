namespace Tooba.Catalog.Domain;

/// <summary>ممیزی تغییر override سیاست رزرو؛ رویدادهای چرخه رزرو را بازنویسی نمی‌کند.</summary>
public sealed class ReservationPolicyAuditEvent
{
    /// <summary>شناسه رویداد.</summary>
    public Guid EventId { get; init; }

    /// <summary>store یا category یا offer.</summary>
    public string Level { get; init; } = "store";

    /// <summary>شناسه سطح؛ فروشگاه تهی است.</summary>
    public Guid? ScopeId { get; init; }

    /// <summary>نام فیلد تغییر کرده.</summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>override قبلی یا inherit.</summary>
    public string? OldOverride { get; init; }

    /// <summary>override جدید یا inherit.</summary>
    public string? NewOverride { get; init; }

    /// <summary>بازیگر Admin.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>زمان ثبت.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>ردیف ممیزی می‌سازد.</summary>
    public static ReservationPolicyAuditEvent Create(
        string level,
        Guid? scopeId,
        string field,
        int? oldOverride,
        int? newOverride,
        Guid actorUserId,
        DateTimeOffset now) =>
        new()
        {
            EventId = Guid.NewGuid(),
            Level = level,
            ScopeId = scopeId,
            Field = field,
            OldOverride = oldOverride?.ToString(),
            NewOverride = newOverride?.ToString(),
            ActorUserId = actorUserId,
            OccurredAt = now,
        };
}
