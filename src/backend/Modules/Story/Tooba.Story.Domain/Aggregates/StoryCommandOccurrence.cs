namespace Tooba.Story.Domain.Aggregates;

/// <summary>رخداد ماندگار فرمان استوری برای بازپخش دقیق ایجاد و چرخهٔ عمر.</summary>
public sealed class StoryCommandOccurrence
{
    /// <summary>کلید پایدار رخداد که کلاینت می‌فرستد.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>tenant مالک.</summary>
    public Guid TenantId { get; init; }

    /// <summary>محدودهٔ اختیاری فروشنده.</summary>
    public Guid? SellerPartyId { get; init; }

    /// <summary>کد خانوادهٔ فرمان (create، submit، approve_publish و مانند آن).</summary>
    public string ActionCode { get; init; } = string.Empty;

    /// <summary>هش SHA-256 بدنهٔ نیت تغییرناپذیر کانونی.</summary>
    public string IntentHash { get; init; } = string.Empty;

    /// <summary>شناسهٔ استوری تولیدشده یا هدف رخداد.</summary>
    public Guid StoryId { get; init; }

    /// <summary>بازیگری که نخستین‌بار رخداد را ثبت کرد.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>لحظهٔ ثبت.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
