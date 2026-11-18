namespace Tooba.Returns.Infrastructure.Persistence;

/// <summary>
/// TB-279: the append-only, versioned decision history of one return case.
/// <para>
/// The legacy <see cref="ReturnDecisionRecord"/> row stays the immutable FIRST decision (the baseline this module
/// shipped) and is never rewritten. Every NEW decision — including an audited correction — is a row here, with a
/// monotonic <see cref="Version"/>, the acting operator, a reviewed reason and the exact reviewed occurrence identity.
/// A correction is the <see cref="ReopenDecision"/> marker: it does not delete or overwrite the decision it corrects,
/// it appends the fact that the case went back to review, and the effective decision becomes "none" until a new
/// decision is recorded.
/// </para>
/// </summary>
public sealed class ReturnDecisionRevision
{
    /// <summary>The stable token of an approval decision.</summary>
    public const string ApproveDecision = "Approve";

    /// <summary>The stable token of a rejection decision.</summary>
    public const string RejectDecision = "Reject";

    /// <summary>The stable token of an audited correction back to review.</summary>
    public const string ReopenDecision = "Reopen";

    /// <summary>پروندهٔ مالک تصمیم.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>شمارهٔ یکتای افزایشی تصمیم در این پرونده (از ۱).</summary>
    public int Version { get; init; }

    /// <summary>Approve | Reject | Reopen.</summary>
    public string Decision { get; init; } = string.Empty;

    /// <summary>دلیل بازبینی‌شدهٔ اپراتور (برای رد و بازگشایی الزامی است).</summary>
    public string? Reason { get; init; }

    /// <summary>اپراتور تصمیم‌گیرنده.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>شناسهٔ رخداد بازبینی‌شده؛ یکتایی تکرار همان اقدام را پاسخ می‌دهد.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>زمان ثبت این تصمیم پایدار.</summary>
    public DateTimeOffset RecordedAt { get; init; }
}
