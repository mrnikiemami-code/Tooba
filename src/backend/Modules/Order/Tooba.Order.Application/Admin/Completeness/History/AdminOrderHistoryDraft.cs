namespace Tooba.Order.Application.Admin.Completeness.History;

/// <summary>
/// سطر تاریخچهٔ ترکیب‌شده پیش از حل برچسب Actor و صفحه‌بندی.
/// </summary>
public sealed record AdminOrderHistoryDraft(
    DateTimeOffset OccurredAt,
    string Kind,
    string LabelFa,
    string LabelEn,
    Guid? ActorUserId,
    string? SummaryFa = null,
    string? SummaryEn = null);

/// <summary>
/// پیشوند یادداشت بازیابی موجودی؛ مالک آن Order است تا ترکیب تاریخچه به Host وابسته نشود.
/// </summary>
public static class AdminOrderInventoryRecoveryNotePrefixes
{
    /// <summary>درخواست بازیابی ثبت شد.</summary>
    public const string Requested = "[inventory_recovery:requested]";

    /// <summary>بازیابی با موفقیت انجام شد.</summary>
    public const string Succeeded = "[inventory_recovery:succeeded]";

    /// <summary>بازیابی به دلیل کمبود موجودی شکست خورد.</summary>
    public const string Failed = "[inventory_recovery:failed_insufficient]";

    /// <summary>بازیابی نیازمند بررسی دستی است.</summary>
    public const string Manual = "[inventory_recovery:requires_manual_review]";
}
