namespace Tooba.AccessControl.Domain.Aggregates;

/// <summary>رویداد حسابرسی Access Control.</summary>
public sealed class AccessAuditEvent
{
    /// <summary>شناسه.</summary>
    public Guid Id { get; set; }

    /// <summary>بازیگر.</summary>
    public Guid ActorUserId { get; set; }

    /// <summary>عمل.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>نوع هدف.</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>شناسهٔ هدف.</summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>محدودهٔ فروشنده.</summary>
    public Guid? SellerScopeId { get; set; }

    /// <summary>خلاصهٔ قبل.</summary>
    public string BeforeSummary { get; set; } = string.Empty;

    /// <summary>خلاصهٔ بعد.</summary>
    public string AfterSummary { get; set; } = string.Empty;

    /// <summary>شناسهٔ ردیابی.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>زمان.</summary>
    public DateTimeOffset At { get; set; }
}
